using CareNirvana.DataAccess;
using CareNirvana.Service.Domain.Model;
using System.Security.Cryptography;
using System.Text;
using Npgsql;
using CareNirvana.Service.Application.Interfaces; // PostgreSQL library

namespace CareNirvana.Service.Infrastructure.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly IAbstractDataLayer _dataLayer;


        public UserRepository(IAbstractDataLayer dataLayer)
        {
            _dataLayer = dataLayer;
        }

        // 🔹 Validate user by checking credentials from PostgreSQL
        public bool ValidateUser(string userName, string password)
        {
            var sql = "SELECT password FROM securityuser WHERE username=@username AND activeflag=true";
            var parameters = new Dictionary<string, object>
            {
                { "@username", userName }
            };

            using var reader = _dataLayer.ExecuteDataReader(sql, parameters);
            if (reader.Read())
            {
                var storedPassword = reader.GetString(0); // Assuming password is in the first column

                // 🔹 You should use **hashed passwords** in production
                return storedPassword == password;
            }

            return false;
        }

        // 🔹 Get user from PostgreSQL
        public SecurityUser? GetUser(string username, string password)
        {
            var secretKey = "your-very-secret-key-1234"; // 16 characters for AES-128
            var iv = "encryptionIntVec"; // 16 characters IV
            var decryptedPassword = DecryptPassword(password, secretKey, iv);

            var sql = "SELECT userid, userdetailid, username, password FROM securityuser WHERE username=@username AND password=@password AND activeflag=true";
            var parameters = new Dictionary<string, object>
            {
                { "@username", username },
                { "@password", decryptedPassword }
            };

            using var reader = _dataLayer.ExecuteDataReader(sql, parameters);
            if (reader.Read())
            {
                return new SecurityUser
                {
                    UserId = reader.GetInt32(0),
                    //UserDetailId = reader.GetInt32(1),
                    UserName = reader.GetString(2),
                    Password = reader.GetString(3) // 🔹 In production, store **hashed passwords**
                };
            }

            return null;
        }

        private string DecryptPassword(string encryptedText, string key, string iv)
        {
            // Convert the key and IV strings to byte arrays.
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] ivBytes = Encoding.UTF8.GetBytes(iv);

            // Convert the encrypted text (Base64 encoded) to a byte array.
            byte[] cipherTextBytes = Convert.FromBase64String(encryptedText);

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = keyBytes;
                aesAlg.IV = ivBytes;
                aesAlg.Mode = CipherMode.CBC;
                aesAlg.Padding = PaddingMode.PKCS7;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(cipherTextBytes))
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                {
                    // Read the decrypted bytes and convert them to a string.
                    string decrypted = srDecrypt.ReadToEnd();
                    return decrypted;
                }
            }
        }

    }
}
