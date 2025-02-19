using CareNirvana.DataAccess;
using CareNirvana.Service.Domain.Model;
using System.Collections.Generic;
using System.Data;
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
            var sql = "SELECT userid, userdetailid, username, password FROM securityuser WHERE username=@username AND activeflag=true";
            var parameters = new Dictionary<string, object>
            {
                { "@username", username }
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
    }
}
