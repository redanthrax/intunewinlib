using System;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntuneWinLib {
    internal static class Encrypt {
        internal static FileEncryptionInfo EncryptFile(string file) {
            // Generate keys and initialization vector
            byte[] encryptionKey = GenerateKey();
            byte[] hmacKey = GenerateKey();
            byte[] iv = GenerateIV();

            // Create a target file path
            string targetFilePath = Path.Combine(Path.GetDirectoryName(file), Guid.NewGuid().ToString());

            // Encrypt the file
            byte[] hmacValue = EncryptFileWithIV(file, targetFilePath, encryptionKey, hmacKey, iv);

            // Compute SHA256 hash of the original file
            byte[] fileDigest;
            using (FileStream fileStream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.None)) {
                using var sha256 = new SHA256Managed();
                fileDigest = sha256.ComputeHash(fileStream);
            }

            // Create and populate FileEncryptionInfo
            FileEncryptionInfo fileEncryptionInfo = new() {
                EncryptionKey = Convert.ToBase64String(encryptionKey),
                MacKey = Convert.ToBase64String(hmacKey),
                InitializationVector = Convert.ToBase64String(iv),
                Mac = Convert.ToBase64String(hmacValue),
                ProfileIdentifier = "ProfileVersion1",
                FileDigest = Convert.ToBase64String(fileDigest),
                FileDigestAlgorithm = "SHA256"
            };

            // Copy the encrypted file back to the original location
            File.Copy(targetFilePath, file, true);
            return fileEncryptionInfo;
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
