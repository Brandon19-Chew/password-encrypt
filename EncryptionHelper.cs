using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace FileEncryptionSystem
{
    public static class EncryptionHelper
    {
        private const int KeySize = 256;
        private const int BlockSize = 128;
        private const int Iterations = 10000;
        private const string FileExtension = ".encrypted";
        private static readonly byte[] Salt = Encoding.UTF8.GetBytes("FileEncryptionSystem2024");

        // Password Management
        public static void SetPassword(string password)
        {
            // Generate a random salt for the password
            byte[] passwordSalt = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(passwordSalt);
            }

            // Hash the password with the salt
            byte[] passwordHash = HashPassword(password, passwordSalt);

            // Save the hash and salt to files
            File.WriteAllBytes("password.hash", passwordHash);
            File.WriteAllBytes("password.salt", passwordSalt);
        }

        public static bool VerifyPassword(string password)
        {
            if (!File.Exists("password.hash") || !File.Exists("password.salt"))
                return false;

            byte[] storedHash = File.ReadAllBytes("password.hash");
            byte[] storedSalt = File.ReadAllBytes("password.salt");

            byte[] inputHash = HashPassword(password, storedSalt);

            // Constant-time comparison to prevent timing attacks
            uint diff = (uint)storedHash.Length ^ (uint)inputHash.Length;
            for (int i = 0; i < storedHash.Length && i < inputHash.Length; i++)
            {
                diff |= (uint)(storedHash[i] ^ inputHash[i]);
            }

            return diff == 0;
        }

        private static byte[] HashPassword(string password, byte[] salt)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(32); // 256-bit key
            }
        }

        // File Encryption
        public static void EncryptFile(string inputFile, string password)
        {
            string outputFile = inputFile + FileExtension;

            using (var passwordBytes = new Rfc2898DeriveBytes(password, Salt, Iterations))
            {
                byte[] keyBytes = passwordBytes.GetBytes(KeySize / 8);
                byte[] ivBytes = passwordBytes.GetBytes(BlockSize / 8);

                using (var aes = Aes.Create())
                {
                    aes.KeySize = KeySize;
                    aes.BlockSize = BlockSize;
                    aes.Key = keyBytes;
                    aes.IV = ivBytes;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (var inputFileStream = new FileStream(inputFile, FileMode.Open, FileAccess.Read))
                    using (var outputFileStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
                    using (var cryptoStream = new CryptoStream(outputFileStream,
                           aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        // Write the original file size at the beginning
                        byte[] fileSizeBytes = BitConverter.GetBytes(inputFileStream.Length);
                        cryptoStream.Write(fileSizeBytes, 0, fileSizeBytes.Length);

                        // Encrypt the file
                        inputFileStream.CopyTo(cryptoStream);
                    }
                }
            }

            // Delete original file after successful encryption
            File.Delete(inputFile);
            MessageBox.Show($"File encrypted successfully!\nSaved as: {Path.GetFileName(outputFile)}",
                "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public static void DecryptFile(string inputFile, string password)
        {
            if (!inputFile.EndsWith(FileExtension))
            {
                MessageBox.Show("This file is not encrypted with this system.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string outputFile = inputFile.Substring(0, inputFile.Length - FileExtension.Length);

            using (var passwordBytes = new Rfc2898DeriveBytes(password, Salt, Iterations))
            {
                byte[] keyBytes = passwordBytes.GetBytes(KeySize / 8);
                byte[] ivBytes = passwordBytes.GetBytes(BlockSize / 8);

                using (var aes = Aes.Create())
                {
                    aes.KeySize = KeySize;
                    aes.BlockSize = BlockSize;
                    aes.Key = keyBytes;
                    aes.IV = ivBytes;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (var inputFileStream = new FileStream(inputFile, FileMode.Open, FileAccess.Read))
                    using (var outputFileStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
                    using (var cryptoStream = new CryptoStream(inputFileStream,
                           aes.CreateDecryptor(), CryptoStreamMode.Read))
                    {
                        // Read the original file size
                        byte[] fileSizeBytes = new byte[8];
                        cryptoStream.Read(fileSizeBytes, 0, 8);

                        // Decrypt the file
                        byte[] buffer = new byte[8192];
                        int bytesRead;

                        while ((bytesRead = cryptoStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            outputFileStream.Write(buffer, 0, bytesRead);
                        }
                    }
                }
            }

            // Delete encrypted file after successful decryption
            File.Delete(inputFile);
            MessageBox.Show($"File decrypted successfully!\nSaved as: {Path.GetFileName(outputFile)}",
                "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // Folder Encryption
        public static void EncryptFolder(string folderPath, string password)
        {
            string[] files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);
            int encryptedCount = 0;

            foreach (string file in files)
            {
                if (!file.EndsWith(FileExtension))
                {
                    try
                    {
                        EncryptFile(file, password);
                        encryptedCount++;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to encrypt {Path.GetFileName(file)}: {ex.Message}",
                            "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }

            MessageBox.Show($"Folder encryption completed!\n{encryptedCount} files encrypted.",
                "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // Folder Decryption
        public static void DecryptFolder(string folderPath, string password)
        {
            string[] files = Directory.GetFiles(folderPath, "*" + FileExtension, SearchOption.AllDirectories);
            int decryptedCount = 0;

            foreach (string file in files)
            {
                try
                {
                    DecryptFile(file, password);
                    decryptedCount++;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to decrypt {Path.GetFileName(file)}: {ex.Message}",
                        "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            MessageBox.Show($"Folder decryption completed!\n{decryptedCount} files decrypted.",
                "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
