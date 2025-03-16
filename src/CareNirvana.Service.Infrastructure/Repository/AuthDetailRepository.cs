using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Npgsql;
using NpgsqlTypes;


namespace CareNirvana.Service.Infrastructure.Repository
{
    public class AuthDetailRepository : IAuthDetailRepository
    {
        private readonly string _connectionString;

        public AuthDetailRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }


        public async Task SaveAsync(AuthDetail authDetail)
        {
            try
            {
                if (authDetail.SaveType == "Add")
                {
                    await InsertAuthDetail(authDetail);
                }
                else if (authDetail.SaveType == "Update")
                {
                    await UpdateAuthDetail(authDetail);
                }
                else if (authDetail.SaveType == "Delete")
                {
                    await DeleteAuthDetail(authDetail);
                }
                //using (var connection = new NpgsqlConnection(_connectionString))
                //{
                //    await connection.OpenAsync();
                //    using (var command = new NpgsqlCommand(
                //        "INSERT INTO authdetail (data, createdon, authnumber) VALUES (@data, @createdon, @authNumber)", connection))
                //    {
                //        // Ensure the data is inserted as a JSONB array
                //        command.Parameters.AddWithValue("@data", NpgsqlDbType.Jsonb | NpgsqlDbType.Array, authDetail.Data.ToArray());

                //        command.Parameters.AddWithValue("@createdon", authDetail.CreatedOn);
                //        command.Parameters.AddWithValue("@authNumber", authDetail.AuthNumber);

                //        await command.ExecuteNonQueryAsync();
                //    }
                //}
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database Error: {ex.Message}");
                throw;
            }
        }

        public async Task InsertAuthDetail(AuthDetail authDetail)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new NpgsqlCommand(
                        "INSERT INTO authdetail (data, createdon, createdby, authnumber, authtypeid, memberid, authduedate, nextreviewdate, treatementtype ) " +
                        "VALUES (@data, @createdon, @createdby, @authNumber, @authTypeId, @memberId, @authDueDate, @nextReviewDate, @treatmentType )", connection))
                    {
                        // Ensure the data is inserted as a JSONB array
                        command.Parameters.AddWithValue("@data", NpgsqlDbType.Jsonb | NpgsqlDbType.Array, authDetail.Data.ToArray());

                        command.Parameters.AddWithValue("@createdon", authDetail.CreatedOn);
                        command.Parameters.AddWithValue("@createdby", authDetail.CreatedBy);
                        command.Parameters.AddWithValue("@authNumber", authDetail.AuthNumber);
                        command.Parameters.AddWithValue("@authTypeId", authDetail.AuthTypeId);
                        command.Parameters.AddWithValue("@memberId", authDetail.MemberId);
                        command.Parameters.AddWithValue("@authDueDate", authDetail.AuthDueDate);
                        command.Parameters.AddWithValue("@nextReviewDate", authDetail.NextReviewDate);
                        command.Parameters.AddWithValue("@treatmentType", authDetail.TreatmentType);

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
                        command.Parameters.AddWithValue("@data", NpgsqlDbType.Jsonb | NpgsqlDbType.Array, authDetail.Data.ToArray());

                        command.Parameters.AddWithValue("@updatedon", authDetail.UpdatedOn);
                        command.Parameters.AddWithValue("@updatedby", authDetail.UpdatedBy);
                        command.Parameters.AddWithValue("@authTypeId", authDetail.AuthTypeId);
                        command.Parameters.AddWithValue("@authDueDate", authDetail.AuthDueDate);
                        command.Parameters.AddWithValue("@nextReviewDate", authDetail.NextReviewDate);
                        command.Parameters.AddWithValue("@treatmentType", authDetail.TreatmentType);
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


    }
}
