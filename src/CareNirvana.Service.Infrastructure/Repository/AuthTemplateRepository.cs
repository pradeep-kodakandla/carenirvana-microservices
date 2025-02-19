using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareNirvana.Service.Infrastructure.Repository
{
    public class AuthTemplateRepository : IAuthTemplateRepository
    {
        private readonly string _connectionString;

        public AuthTemplateRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }


        public async Task<List<AuthTemplate>> GetAllAsync()
        {
            var templates = new List<AuthTemplate>();
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new NpgsqlCommand("SELECT id, templatename, jsoncontent, createdon, createdby FROM authtemplate", connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        templates.Add(new AuthTemplate
                        {
                            Id = reader.GetInt32(0),
                            TemplateName = reader.GetString(1),
                            JsonContent = reader.GetString(2),
                            CreatedOn = reader.GetDateTime(3),
                            CreatedBy = reader.GetInt32(4)
                        });
                    }
                }
            }
            return templates;
        }
    }
}
