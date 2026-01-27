using CareNirvana.DataAccess;
using CareNirvana.Service.Domain.Model;
using System.Security.Cryptography;
using System.Text;
using Npgsql;
using CareNirvana.Service.Application.Interfaces;
using System.Data; // PostgreSQL library

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
            var secretKey = "0123456789ABCDEF"; // Exactly 16 characters = 16 bytes in UTF8
            var iv = "encryptionIntVec";       // Also ensure the IV is exactly 16 characters

            var decryptedPassword = DecryptPassword(password, secretKey, iv);
            var sql = "SELECT userid, su.userdetailid, LTRIM(RTRIM(CONCAT(sd.firstname, ' ', sd.lastname, ' - ', cr.name))) AS username, password FROM securityuser su" +
                " JOIN securityuserdetail sd ON sd.userdetailid = su.userdetailid" +
                " JOIN cfgrole cr ON cr.role_id = sd.roleid" +
                " WHERE LOWER(username) = LOWER(@username) AND password=@password AND su.activeflag=true AND su.deletedon IS NULL";

            //var sql = "SELECT userid, userdetailid, username, password FROM securityuser WHERE LOWER(username) = LOWER(@username) AND password=@password AND activeflag=true";
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
            // Convert the key and IV strings to byte arrays using UTF8 encoding.
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] ivBytes = Encoding.UTF8.GetBytes(iv);

            // Ensure key length is valid: 16, 24, or 32 bytes.
            if (!(keyBytes.Length == 16 || keyBytes.Length == 24 || keyBytes.Length == 32))
            {
                throw new ArgumentException("Invalid key size. Key must be 16, 24, or 32 bytes for AES.");
            }

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
                    // Read the decrypted bytes and return the original string.
                    return srDecrypt.ReadToEnd();
                }
            }
        }

        public async Task<List<SecurityUser>> GetUserDetails()
        {
            var securityUser = new List<SecurityUser>();

            try
            {
                {
                    var sql =
                      "SELECT LTRIM(RTRIM(CONCAT(sd.firstname, ' ', sd.lastname, ' - ', cr.name))) AS username, " +
                      "su.userid " +
                      "FROM securityuser su " +
                      "JOIN securityuserdetail sd ON sd.userdetailid = su.userdetailid " +
                      "JOIN cfgrole cr ON cr.role_id = sd.roleid " +
                      "WHERE su.deletedon IS NULL";


                    using (var reader = _dataLayer.ExecuteDataReader(sql))
                    {
                        while (reader.Read())
                        {
                            securityUser.Add(new SecurityUser
                            {
                                UserName = reader.GetString(0),
                                UserId = reader.GetInt32(1)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database Error: {ex.Message}");
                throw;
            }
            return securityUser;
        }

        public List<SecurityUser> GetAll()
        {
            var list = new List<SecurityUser>();

            var sql = @"SELECT u.*, d.* 
                        FROM securityuser u 
                        JOIN securityuserdetail d ON u.userdetailid = d.userdetailid 
                        WHERE u.deletedon IS NULL";

            using (var reader = _dataLayer.ExecuteDataReader(sql))
            {
                while (reader.Read())
                {
                    list.Add(MapToDto(reader));
                }
            }

            return list;
        }

        public SecurityUser? GetById(int userId)
        {
            var sql = @"SELECT u.*, d.* 
                        FROM securityuser u 
                        JOIN securityuserdetail d ON u.userdetailid = d.userdetailid 
                        WHERE u.userid = @userId";

            var parameters = new Dictionary<string, object>
            {
                { "@userId", userId }
            };

            using var reader = _dataLayer.ExecuteDataReader(sql, parameters);
            return reader.Read() ? MapToDto(reader) : null;
        }

        public int Add(SecurityUser dto)
        {
            var detailSql = @"INSERT INTO securityuserdetail 
                (title, firstname, lastname, middlename, suffix, credentials, roleid, dateofstarting, 
                 clinicname, speciality, timezoneid, ratetypeid, rate, managerid, primaryemail, alternatemail, 
                 primaryphone, primaryphoneextension, alternatephone, alternatephoneextension, mobilephone, 
                 signature, islocked, activeflag, createdon, createdby)
                VALUES
                (@title, @firstname, @lastname, @middlename, @suffix, @credentials, @roleid, @dateofstarting,
                 @clinicname, @speciality, @timezoneid, @ratetypeid, @rate, @managerid, @primaryemail, @alternatemail,
                 @primaryphone, @primaryphoneextension, @alternatephone, @alternatephoneextension, @mobilephone,
                 @signature, @islocked, true, CURRENT_TIMESTAMP, @createdby)
                RETURNING userdetailid";

            // Replace the problematic dictionary initialization in Add(SecurityUser dto) with the following:

            var detailParams = new Dictionary<string, object>
            {
                { "@title", dto.UserDetail.Title },
                { "@firstname", dto.UserDetail.FirstName },
                { "@lastname", dto.UserDetail.LastName },
                { "@middlename", dto.UserDetail.MiddleName },
                { "@suffix", dto.UserDetail.Suffix },
                { "@credentials", dto.UserDetail.Credentials },
                { "@roleid", dto.UserDetail.Role?.RoleId ?? (object)DBNull.Value },
                { "@dateofstarting", dto.UserDetail.DateOfStarting },
                { "@clinicname", dto.UserDetail.ClinicName },
                { "@speciality", dto.UserDetail.Speciality },
                { "@timezoneid", dto.UserDetail.TimeZone?.TimeZoneId ?? (object)DBNull.Value },
                { "@ratetypeid", dto.UserDetail.RateType?.RateTypeId ?? (object)DBNull.Value },
                { "@rate", dto.UserDetail.Rate },
                { "@managerid", dto.UserDetail.ManagerId },
                { "@primaryemail", dto.UserDetail.PrimaryEmail },
                { "@alternatemail", dto.UserDetail.AlternateEmail },
                { "@primaryphone", dto.UserDetail.PrimaryPhone },
                { "@primaryphoneextension", dto.UserDetail.PrimayPhoneExtension },
                { "@alternatephone", dto.UserDetail.AlternatePhone },
                { "@alternatephoneextension", dto.UserDetail.AlternatePhoneExtension },
                { "@mobilephone", dto.UserDetail.MobilePhone },
                { "@signature", dto.UserDetail.Signature ?? (object)DBNull.Value },
                { "@islocked", dto.UserDetail.IsLocked },
                { "@createdby", dto.UserDetail.ActiveFlag } // If you have a CreatedBy property, use it; otherwise, adjust accordingly
            };

            var userDetailId = Convert.ToInt32(_dataLayer.ExecuteScalar(detailSql, detailParams));

            var userSql = @"INSERT INTO securityuser 
                (userdetailid, username, password, usertypeid, activeflag, createdon, createdby)
                VALUES
                (@userdetailid, @username, @password, @usertypeid, true, CURRENT_TIMESTAMP, @createdby)
                RETURNING userid";

            var userParams = new Dictionary<string, object>
            {
                { "@userdetailid", userDetailId },
                { "@username", dto.UserName },
                { "@password", dto.Password },
                { "@usertypeid", dto.UserType },
                { "@createdby", dto.CreatedBy }
            };

            return Convert.ToInt32(_dataLayer.ExecuteScalar(userSql, userParams));
        }

        public void Update(SecurityUser dto)
        {
            var userSql = @"UPDATE securityuser SET
                username = @username, password = @password, usertypeid = @usertypeid,
                updatedon = CURRENT_TIMESTAMP, updatedby = @updatedby
                WHERE userid = @userid";

            var userParams = new Dictionary<string, object>
            {
                { "@username", dto.UserName },
                { "@password", dto.Password },
                { "@usertypeid", dto.UserType?.UserTypeId ?? (object)DBNull.Value },
                { "@updatedby", dto.UpdatedBy ?? (object)DBNull.Value },
                { "@userid", dto.UserId }
            };

            _dataLayer.ExectuteNonQuery(userSql, userParams);

            var detailSql = @"UPDATE securityuserdetail SET
                firstname = @firstname, lastname = @lastname, speciality = @speciality,
                updatedon = CURRENT_TIMESTAMP, updatedby = @updatedby
                WHERE userdetailid = @userdetailid";

            var detailParams = new Dictionary<string, object>
            {
                { "@firstname", dto.UserDetail.FirstName },
                { "@lastname", dto.UserDetail.LastName },
                { "@speciality", dto.UserDetail.Speciality },
                { "@updatedby", dto.UpdatedBy ?? (object)DBNull.Value },
                { "@userdetailid", dto.UserDetail.UserDetailId }
            };

            _dataLayer.ExectuteNonQuery(detailSql, detailParams);
        }

        public void Delete(int userId, int deletedBy)
        {
            var getDetailIdSql = "SELECT userdetailid FROM securityuser WHERE userid = @userid";
            var detailParams = new Dictionary<string, object> { { "@userid", userId } };
            var userDetailId = Convert.ToInt32(_dataLayer.ExecuteScalar(getDetailIdSql, detailParams));

            var userSql = @"UPDATE securityuser SET 
                activeflag = false, deletedon = CURRENT_TIMESTAMP, deletedby = @deletedby 
                WHERE userid = @userid";

            _dataLayer.ExectuteNonQuery(userSql, new Dictionary<string, object>
            {
                { "@userid", userId },
                { "@deletedby", deletedBy }
            });

            var detailSql = @"UPDATE securityuserdetail SET 
                activeflag = false, deletedon = CURRENT_TIMESTAMP, deletedby = @deletedby 
                WHERE userdetailid = @userdetailid";

            _dataLayer.ExectuteNonQuery(detailSql, new Dictionary<string, object>
            {
                { "@userdetailid", userDetailId },
                { "@deletedby", deletedBy }
            });
        }

        private SecurityUser MapToDto(IDataRecord r)
        {
            return new SecurityUser
            {
                UserId = r.GetInt32(r.GetOrdinal("userid")),
                UserName = r.GetString(r.GetOrdinal("username")),
                Password = r.GetString(r.GetOrdinal("password")),
                UserType = new CfgUserType
                {
                    UserTypeId = r.GetInt32(r.GetOrdinal("usertypeid")),
                    UserTypeName = "", // Populate if available in the result set
                    ActiveFlag = true  // Populate if available in the result set
                },
                ActiveFlag = r.GetBoolean(r.GetOrdinal("activeflag")),
                CreatedOn = r.GetDateTime(r.GetOrdinal("createdon")),
                CreatedBy = r.GetInt32(r.GetOrdinal("createdby")),
                UpdatedOn = r.IsDBNull(r.GetOrdinal("updatedon")) ? null : r.GetDateTime(r.GetOrdinal("updatedon")),
                UpdatedBy = r.IsDBNull(r.GetOrdinal("updatedby")) ? null : r.GetInt32(r.GetOrdinal("updatedby")),
                DeletedOn = r.IsDBNull(r.GetOrdinal("deletedon")) ? null : r.GetDateTime(r.GetOrdinal("deletedon")),
                DeletedBy = r.IsDBNull(r.GetOrdinal("deletedby")) ? null : r.GetInt32(r.GetOrdinal("deletedby")),
                UserDetail = new SecurityUserDetail
                {
                    UserDetailId = r.GetInt32(r.GetOrdinal("userdetailid")),
                    FirstName = r.GetString(r.GetOrdinal("firstname")),
                    LastName = r.GetString(r.GetOrdinal("lastname")),
                    Speciality = r.GetString(r.GetOrdinal("speciality")),
                    // Populate other properties as needed, checking for nulls where appropriate
                }
            };
        }

        public async Task<IEnumerable<SecurityUser>> GetAllAsync()
        {
            return await Task.FromResult(GetAll());
        }

        public async Task<SecurityUser?> GetByIdAsync(int userId)
        {
            return await Task.FromResult(GetById(userId));
        }

        public async Task<int> AddAsync(SecurityUser user)
        {
            return await Task.FromResult(Add(user));
        }

        public async Task UpdateAsync(SecurityUser user)
        {
            Update(user);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(int userId, int deletedBy)
        {
            Delete(userId, deletedBy);
            await Task.CompletedTask;
        }

    }

}

