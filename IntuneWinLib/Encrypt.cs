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

        internal static bool DecryptFile(string encryptedFile, FileEncryptionInfo encryptionInfo) {
            // Extract encryption and MAC keys, and initialization vector from encryptionInfo
            byte[] key1 = Convert.FromBase64String(encryptionInfo.EncryptionKey);
            byte[] key2 = Convert.FromBase64String(encryptionInfo.MacKey);
            byte[] iv = Convert.FromBase64String(encryptionInfo.InitializationVector);

            // Create a temporary file path for the decrypted file
            string tempFilePath = Path.Combine(Path.GetDirectoryName(encryptedFile) ?? string.Empty, Guid.NewGuid().ToString());

            // Decrypt the file
            bool isDecryptionSuccessful = DecryptFileWithIV(encryptedFile, tempFilePath, key1, key2, iv);
            if (!isDecryptionSuccessful) {
                return false;
            }

            // Validate file integrity using SHA256 hash
            byte[] fileDigest;
            using (FileStream fileStream = File.Open(tempFilePath, FileMode.Open, FileAccess.Read, FileShare.None)) {
                var sha256Calculator = new SHA256WithBufferSize();
                fileDigest = sha256Calculator.ComputeHash(fileStream, 2097152);
            }

            string computedDigest = Convert.ToBase64String(fileDigest);
            if (computedDigest != encryptionInfo.FileDigest) {
                // File digest doesn't match, indicating the file may be corrupted or tampered with
                return false;
            }

            // Copy the decrypted file back to the original location
            File.Copy(tempFilePath, encryptedFile, true);
            File.Delete(tempFilePath);

            return true;
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

        private static bool DecryptFileWithIV(string encryptedFile, string outputFile, byte[] encryptionKey, byte[] hmacKey, byte[] expectedIV) {
            using (Aes aes = Aes.Create()) {
                var hmac = new HMACSHA256(hmacKey);
                int hmacSize = hmac.HashSize / 8;
                byte[] buffer = new byte[2097152];

                using (var encryptedStream = new FileStream(encryptedFile, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    // Read HMAC and IV from the encrypted file
                    byte[] fileHmac = new byte[hmacSize];
                    encryptedStream.Read(fileHmac, 0, hmacSize);

                    byte[] iv = new byte[aes.BlockSize / 8];
                    encryptedStream.Read(iv, 0, iv.Length);

                    // Check if the IV matches
                    if (!iv.SequenceEqual(expectedIV)) {
                        return false; // IV doesn't match, file might be tampered
                    }

                    using (var decryptor = aes.CreateDecryptor(encryptionKey, iv))
                    using (var decryptedStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write, FileShare.None))
                    using (var cryptoStream = new CryptoStream(decryptedStream, decryptor, CryptoStreamMode.Write)) {
                        int bytesRead;
                        while ((bytesRead = encryptedStream.Read(buffer, 0, buffer.Length)) > 0) {
                            cryptoStream.Write(buffer, 0, bytesRead);
                        }
                        cryptoStream.FlushFinalBlock();
                    }

                    // Validate HMAC for integrity check
                    using (var outputFileStream = new FileStream(outputFile, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                        byte[] computedHmac = hmac.ComputeHash(outputFileStream);
                    }
                }
            }

            return true;
        }

    }
}
