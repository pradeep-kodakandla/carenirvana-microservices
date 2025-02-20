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
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new NpgsqlCommand(
                        "INSERT INTO authdetailsave (data, createdon,authnumber) VALUES (@data, @createdon, @authNumber)", connection))
                    {
                        // Ensure the data is inserted as a JSONB array
                        command.Parameters.AddWithValue("@data", NpgsqlDbType.Jsonb | NpgsqlDbType.Array, authDetail.Data.ToArray());

                        command.Parameters.AddWithValue("@createdon", authDetail.CreatedOn);
                        command.Parameters.AddWithValue("@authNumber", authDetail.AuthNumber);

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
