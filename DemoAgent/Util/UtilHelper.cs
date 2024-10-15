using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Util
{
    public class UtilHelper
    {
        public static string GeneratePrivateKeyFromUsername(string username, string privateKey)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                // Kết hợp tên người dùng với khóa riêng gốc
                string combined = username + privateKey;
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));

                // Chuyển đổi băm thành chuỗi Base64
                return Convert.ToBase64String(hashBytes);
            }
        }

        public static void EncryptFile(string inputFilePath, string outputFilePath, string publicKeyPem)
        {
            using (Aes aes = Aes.Create())
            {
                aes.GenerateKey();
                aes.GenerateIV();
                byte[] key = aes.Key;
                byte[] iv = aes.IV;

                byte[] publicKeyBytes = Convert.FromBase64String(publicKeyPem);
                byte[] encryptedKey;
                using (RSA rsa = RSA.Create())
                {
                    try
                    {
                        rsa.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);
                        encryptedKey = rsa.Encrypt(key, RSAEncryptionPadding.OaepSHA256);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(ex.Message);
                    }
                }
                using (FileStream inputFileStream = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read))
                using (FileStream outputFileStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write))
                {
                    outputFileStream.Write(BitConverter.GetBytes(encryptedKey.Length), 0, 4);
                    outputFileStream.Write(encryptedKey, 0, encryptedKey.Length);
                    outputFileStream.Write(iv, 0, iv.Length);

                    ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                    using (CryptoStream csEncrypt = new CryptoStream(outputFileStream, encryptor, CryptoStreamMode.Write))
                    {
                        byte[] buffer = new byte[1048576]; // Bộ đệm 1MB
                        int bytesRead;
                        while ((bytesRead = inputFileStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            csEncrypt.Write(buffer, 0, bytesRead);
                        }
                    }
                }
            }
        }

        public static void DecryptFile(string inputFilePath, string outputFilePath, string privateKeyPem)
        {
            using (FileStream inputFileStream = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read))
            {
                // Đọc chiều dài của khóa AES đã mã hóa
                byte[] lengthBytes = new byte[4];
                inputFileStream.Read(lengthBytes, 0, 4);
                int keyLength = BitConverter.ToInt32(lengthBytes, 0);

                // Đọc khóa AES đã mã hóa
                byte[] encryptedKey = new byte[keyLength];
                inputFileStream.Read(encryptedKey, 0, encryptedKey.Length);

                // Giải mã khóa AES bằng RSA với khóa riêng
                byte[]? key = null;
                using (RSA rsa = RSA.Create())
                {
                    try
                    {
                        rsa.ImportRSAPrivateKey(Convert.FromBase64String(privateKeyPem), out _);
                        key = rsa.Decrypt(encryptedKey, RSAEncryptionPadding.OaepSHA256);
                    }
                    catch (CryptographicException ex)
                    {
                    }
                }

                // Đọc IV
                byte[] iv = new byte[16];
                inputFileStream.Read(iv, 0, iv.Length);

                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.Key = key;
                    aesAlg.IV = iv;

                    using (FileStream outputFileStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write))
                    {
                        ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                        using (CryptoStream csDecrypt = new CryptoStream(inputFileStream, decryptor, CryptoStreamMode.Read))
                        {
                            byte[] buffer = new byte[1048576]; // Bộ đệm 1MB
                            int bytesRead;
                            while ((bytesRead = csDecrypt.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                outputFileStream.Write(buffer, 0, bytesRead);
                            }
                        }
                    }
                }
            }
        }

        public static string getRSAPublicKey()
        {
            using (RSA rsa = RSA.Create())
            {
                rsa.KeySize = 2048;
                return Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo());
            };

        }
        public static string getRSAPrivateKey() { 
            using (RSA rsa = RSA.Create())
            {
                rsa.KeySize = 2048;
                return Convert.ToBase64String(rsa.ExportRSAPrivateKey());
            };

        }

        public static DateTime? TryParseDate(string dateString)
        {
            if(DateTime.TryParse(dateString, out DateTime date))
            {
                return date;
            }
            return null;
        }


    }
}
