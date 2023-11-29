using System;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntuneWinLib {
    internal static class Encrypt {
        internal static FileEncryptionInfo EncryptFile(string file) {
            // Generate encryption and MAC keys, and initialization vector
            byte[] key1 = GenerateKey();
            byte[] key2 = GenerateKey();
            byte[] iv = GenerateIV();

            // Create a temporary file path for the encrypted file
            string tempFilePath = Path.Combine(Path.GetDirectoryName(file) ?? string.Empty, Guid.NewGuid().ToString());

            // Encrypt the file
            byte[] mac = EncryptFileWithIV(file, tempFilePath, key1, key2, iv);

            // Compute SHA256 hash for the file
            byte[] fileDigest;
            using (FileStream fileStream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.None)) {
                var sha256Calculator = new SHA256WithBufferSize();
                fileDigest = sha256Calculator.ComputeHash(fileStream, 2097152);
            }

            // Construct FileEncryptionInfo object
            var fileEncryptionInfo = new FileEncryptionInfo {
                EncryptionKey = Convert.ToBase64String(key1),
                MacKey = Convert.ToBase64String(key2),
                InitializationVector = Convert.ToBase64String(iv),
                Mac = Convert.ToBase64String(mac),
                ProfileIdentifier = "ProfileVersion1",
                FileDigest = Convert.ToBase64String(fileDigest),
                FileDigestAlgorithm = "SHA256"
            };

            // Copy the encrypted file back to the original location
            File.Copy(tempFilePath, file, true);
            File.Delete(tempFilePath);

            return fileEncryptionInfo;
        }

        private class SHA256WithBufferSize {
            public byte[] ComputeHash(Stream inputStream, int bufferSize) {
                using var sha256 = SHA256.Create();
                var buffer = new byte[bufferSize];
                int bytesRead;

                while ((bytesRead = inputStream.Read(buffer, 0, bufferSize)) > 0) {
                    sha256.TransformBlock(buffer, 0, bytesRead, null, 0);
                }

                sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                return sha256.Hash;
            }
        }


        private static byte[] GenerateKey() {
            using (var aes = Aes.Create()) {
                aes.GenerateKey();
                return aes.Key;
            }
        }

        private static byte[] GenerateIV() {
            using(var aes = Aes.Create()) {
                return aes.IV;
            }
        }

        private static byte[] EncryptFileWithIV(string sourceFile, string targetFile, byte[] encryptionKey, byte[] hmacKey, byte[] initializationVector) {
            byte[] hashValue = null;
            using (Aes aes = Aes.Create()) {
                var hmac = new HMACSHA256(hmacKey);
                int hmacSize = hmac.HashSize / 8;
                byte[] buffer = new byte[2097152];

                using (var targetStream = new FileStream(targetFile, FileMode.Create, FileAccess.Write, FileShare.None)) {
                    targetStream.Write(new byte[hmacSize + initializationVector.Length], 0, hmacSize + initializationVector.Length);

                    using (var encryptor = aes.CreateEncryptor(encryptionKey, initializationVector))
                    using (var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                    using (var cryptoStream = new CryptoStream(targetStream, encryptor, CryptoStreamMode.Write)) {
                        int bytesRead;
                        while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0) {
                            cryptoStream.Write(buffer, 0, bytesRead);
                        }
                        cryptoStream.FlushFinalBlock();
                    }

                    using (var fileStream = new FileStream(targetFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) {
                        fileStream.Seek(hmacSize, SeekOrigin.Begin);
                        fileStream.Write(initializationVector, 0, initializationVector.Length);
                        fileStream.Seek(hmacSize, SeekOrigin.Begin);

                        hashValue = hmac.ComputeHash(fileStream);
                        fileStream.Seek(0, SeekOrigin.Begin);
                        fileStream.Write(hashValue, 0, hashValue.Length);
                    }
                }
            }

            return hashValue;
        }
    }
}
