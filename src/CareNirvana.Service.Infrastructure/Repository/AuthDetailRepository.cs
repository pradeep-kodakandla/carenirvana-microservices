using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Npgsql;
using NpgsqlTypes;
using System.ComponentModel;
using System.Text.Json;  // Required for JsonElement


namespace CareNirvana.Service.Infrastructure.Repository
{
    public class AuthDetailRepository : IAuthDetailRepository
    {
        private readonly string _connectionString;

        public AuthDetailRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<long?> SaveAsync(AuthDetail authDetail)
        {
            try
            {
                if (authDetail.SaveType == "Add")
                {
                    var insertedId = await InsertAuthDetail(authDetail);
                    return insertedId;
                }
                else if (authDetail.SaveType == "Update")
                {
                    await UpdateAuthDetail(authDetail);
                    return null;
                }
                else if (authDetail.SaveType == "Delete")
                {
                    await DeleteAuthDetail(authDetail);
                    return null;
                }
                else
                {
                    throw new InvalidOperationException("Invalid SaveType: " + authDetail.SaveType);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database Error: {ex.Message}");
                throw;
            }
        }

        public async Task<long> InsertAuthDetail(AuthDetail authDetail)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new NpgsqlCommand(
                                    @"INSERT INTO authdetail 
                                    (data, createdon, createdby, authnumber, authtypeid, memberid, authduedate, nextreviewdate, treatementtype, authclassid, authassignedto)
                                    VALUES 
                                    (@data, @createdon, @createdby, @authNumber, @authTypeId, @memberId, @authDueDate, @nextReviewDate, @treatmentType, @authclassid, @authassignedto)
                                    RETURNING authdetailid;", connection))
                    {
                        // Convert objects inside List<object> to proper JSON-compatible objects
                        var processedData = authDetail.Data.Select(item =>
                        {
                            if (item is JsonElement element) // Check if it's a JsonElement
                            {
                                return JsonConvert.DeserializeObject<object>(element.GetRawText()); // Convert to object
                            }
                            return item;
                        }).ToList();

                        var jsonData = JsonConvert.SerializeObject(processedData, Formatting.None);

                        command.Parameters.AddWithValue("@data", NpgsqlDbType.Jsonb, jsonData);
                        command.Parameters.AddWithValue("@createdon", authDetail.CreatedOn);
                        command.Parameters.AddWithValue("@createdby", authDetail.CreatedBy);
                        command.Parameters.AddWithValue("@authNumber", authDetail.AuthNumber);
                        command.Parameters.AddWithValue("@authTypeId", authDetail.AuthTypeId);
                        command.Parameters.AddWithValue("@memberId", authDetail.MemberId);
                        command.Parameters.AddWithValue("@authDueDate", authDetail.AuthDueDate);
                        command.Parameters.AddWithValue("@nextReviewDate", authDetail.NextReviewDate);
                        command.Parameters.AddWithValue("@treatmentType", authDetail.TreatmentType);
                        command.Parameters.AddWithValue("@authclassid", authDetail.AuthClassId);
                        command.Parameters.AddWithValue("@authassignedto", 1);
                        var result = await command.ExecuteScalarAsync();
                        long insertedId = Convert.ToInt64(result);
                        return insertedId;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database Error: {ex.Message}");
                throw;
            }
        }

        public async Task UpdateAuthDetail(AuthDetail authDetail)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new NpgsqlCommand(
                     "UPDATE authdetail " +
                     "SET data = @data, " +
                     "    updatedon = @updatedon, " +
                     "    updatedby = @updatedby, " +
                     "    authtypeid = @authTypeId, " +
                     "    authduedate = @authDueDate, " +
                     "    nextreviewdate = @nextReviewDate, " +
                     "    treatementtype = @treatmentType " +
                     "WHERE authnumber = @authNumber", connection))
                    {
                        // Ensure the data is inserted as a JSONB array
                        // Convert objects inside List<object> to proper JSON-compatible objects
                        var processedData = authDetail.Data.Select(item =>
                        {
                            if (item is JsonElement element) // Check if it's a JsonElement
                            {
                                return JsonConvert.DeserializeObject<object>(element.GetRawText()); // Convert to object
                            }
                            return item; // Keep as-is if it's already a valid object
                        }).ToList();

                        // Serialize the cleaned data properly
                        var jsonData = JsonConvert.SerializeObject(processedData, Formatting.None);

                        // Insert properly formatted JSONB into PostgreSQL
                        command.Parameters.AddWithValue("@data", NpgsqlDbType.Jsonb, jsonData);

                        command.Parameters.AddWithValue("@updatedon", authDetail.UpdatedOn);
                        command.Parameters.AddWithValue("@updatedby", authDetail.UpdatedBy);
                        command.Parameters.AddWithValue("@authTypeId", authDetail.AuthTypeId);
                        command.Parameters.AddWithValue("@authDueDate", authDetail.AuthDueDate);
                        command.Parameters.AddWithValue("@nextReviewDate", authDetail.NextReviewDate);
                        command.Parameters.AddWithValue("@treatmentType", authDetail.TreatmentType);
                        command.Parameters.AddWithValue("@authNumber", authDetail.AuthNumber); // Used in WHERE condition
                        command.Parameters.AddWithValue("@authassignedto", 1);

                        await command.ExecuteNonQueryAsync();
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database Error: {ex.Message}");
                throw;
            }
        }

        public async Task DeleteAuthDetail(AuthDetail authDetail)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new NpgsqlCommand(
                     "UPDATE authdetail " +
                     "SET deletedon = @deletedon, " +
                     "    deletedby = @deletedby, " +
                     "WHERE authnumber = @authNumber", connection))
                    {
                        // Ensure the data is inserted as a JSONB array
                        command.Parameters.AddWithValue("@data", NpgsqlDbType.Jsonb | NpgsqlDbType.Array, authDetail.Data.ToArray());

                        command.Parameters.AddWithValue("@deletedon", authDetail.DeletedOn);
                        command.Parameters.AddWithValue("@deletedby", authDetail.DeletedBy);

                        command.Parameters.AddWithValue("@authNumber", authDetail.AuthNumber); // Used in WHERE condition

                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database Error: {ex.Message}");
                throw;
            }
        }

        public async Task<List<AuthDetail>> GetAllAsync(int memberId)
        {
            var authDetails = new List<AuthDetail>();

            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new NpgsqlCommand(
                        "SELECT authdetailid, authnumber, authtypeid, memberid, authduedate, nextreviewdate, treatementtype, data, createdon, createdby, updatedon, updatedby, deletedon, deletedby, authclassid, authassignedto " +
                        "FROM authdetail WHERE memberid = @memberId AND deletedon IS NULL", connection))
                    {
                        command.Parameters.AddWithValue("@memberId", memberId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                authDetails.Add(new AuthDetail
                                {
                                    Id = reader.GetInt32(0),
                                    AuthNumber = reader.GetString(1),
                                    AuthTypeId = reader.GetInt32(2),
                                    MemberId = reader.GetInt32(3),
                                    AuthDueDate = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                                    NextReviewDate = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                                    TreatmentType = reader.IsDBNull(6) ? null : reader.GetString(6),
                                    //Data = reader.IsDBNull(7) ? null : JsonConvert.DeserializeObject<List<object>>(reader.GetString(7)),
                                    CreatedOn = reader.GetDateTime(8),
                                    CreatedBy = reader.IsDBNull(9) ? null : reader.GetInt32(9),
                                    UpdatedOn = reader.IsDBNull(10) ? null : reader.GetDateTime(10),
                                    UpdatedBy = reader.IsDBNull(11) ? null : reader.GetInt32(11),
                                    DeletedOn = reader.IsDBNull(12) ? null : reader.GetDateTime(12),
                                    DeletedBy = reader.IsDBNull(13) ? null : reader.GetInt32(13),
                                    AuthClassId = reader.IsDBNull(14) ? null : reader.GetInt32(14),
                                    AuthAssignedTo = reader.IsDBNull(15) ? null : reader.GetInt32(15)
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database Error: {ex.Message}");
                throw;
            }

            return authDetails;
        }

        public async Task<List<AuthDetail>> GetAuthData(string authNumber)
        {
            var authDetails = new List<AuthDetail>();

            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new NpgsqlCommand(
                        "SELECT authdetailid, authnumber, authtypeid, memberid, authduedate, nextreviewdate, treatementtype, data, createdon, createdby, updatedon, updatedby, deletedon, deletedby, authclassid, authassignedto " +
                        "FROM authdetail WHERE authnumber = @authNumber AND deletedon IS NULL", connection))
                    {
                        command.Parameters.AddWithValue("@authNumber", authNumber);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                authDetails.Add(new AuthDetail
                                {
                                    Id = reader.GetInt32(0),
                                    AuthNumber = reader.GetString(1),
                                    AuthTypeId = reader.GetInt32(2),
                                    MemberId = reader.GetInt32(3),
                                    AuthDueDate = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                                    NextReviewDate = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                                    TreatmentType = reader.IsDBNull(6) ? null : reader.GetString(6),
                                    responseData = reader.IsDBNull(7) ? null : reader.GetString(7),
                                    CreatedOn = reader.GetDateTime(8),
                                    CreatedBy = reader.IsDBNull(9) ? null : reader.GetInt32(9),
                                    UpdatedOn = reader.IsDBNull(10) ? null : reader.GetDateTime(10),
                                    UpdatedBy = reader.IsDBNull(11) ? null : reader.GetInt32(11),
                                    DeletedOn = reader.IsDBNull(12) ? null : reader.GetDateTime(12),
                                    DeletedBy = reader.IsDBNull(13) ? null : reader.GetInt32(13),
                                    AuthClassId = reader.IsDBNull(14) ? null : reader.GetInt32(14),
                                    AuthAssignedTo = reader.IsDBNull(15) ? null : reader.GetInt32(15)
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database Error: {ex.Message}");
                throw;
            }
            return authDetails;
        }
    }
}