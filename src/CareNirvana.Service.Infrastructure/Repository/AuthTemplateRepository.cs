using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using Microsoft.Extensions.Configuration;
using Npgsql;
using NpgsqlTypes;
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


        public async Task<List<AuthTemplate>> GetAllAsync(int classId)
        {
            var templates = new List<AuthTemplate>();

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = "SELECT id, templatename, createdon, createdby, authclassid FROM authtemplate";
                if (classId > 0)
                {
                    query += " WHERE authclassid = @classid";
                }

                using (var command = new NpgsqlCommand(query, connection))
                {
                    if (classId > 0)
                    {
                        command.Parameters.AddWithValue("@classid", classId);
                    }

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            templates.Add(new AuthTemplate
                            {
                                Id = reader.GetInt32(0),
                                TemplateName = reader.GetString(1),
                                CreatedOn = reader.GetDateTime(2),
                                CreatedBy = reader.GetInt32(3),
                                authclassid = reader.IsDBNull(4) ? null : reader.GetInt32(4)
                            });
                        }
                    }
                }
            }

            return templates;
        }


        public async Task<List<AuthTemplate>> GetAuthTemplate(int id)
        {
            var templates = new List<AuthTemplate>();
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                // Add WHERE clause with parameterized query
                using (var command = new NpgsqlCommand("SELECT id, templatename, jsoncontent, createdon, createdby, authclassid FROM authtemplate WHERE id = @id", connection))
                {
                    // Add parameter for id
                    command.Parameters.AddWithValue("@id", id);

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
                                CreatedBy = reader.GetInt32(4),
                                authclassid = reader.IsDBNull(5) ? null : reader.GetInt32(5)
                            });
                        }
                    }
                }
            }
            return templates;
        }

        public async Task SaveAsync(AuthTemplate authTemplate)
        {
            try
            {
                if (authTemplate.Id == 0)
                {
                    using (var connection = new NpgsqlConnection(_connectionString))
                    {
                        await connection.OpenAsync();
                        using (var command = new NpgsqlCommand(
                            "INSERT INTO authtemplate (jsoncontent, createdby, createdon,templatename, authclassid) VALUES (@JsonContent::jsonb, @createdby,@createdon, @templateName, @authclassid)", connection))
                        {
                            // Ensure the data is inserted as a JSONB array
                            command.Parameters.AddWithValue("@JsonContent", authTemplate.JsonContent);
                            command.Parameters.AddWithValue("@createdby", authTemplate.CreatedBy);
                            command.Parameters.AddWithValue("@createdon", authTemplate.CreatedOn);
                            command.Parameters.AddWithValue("@templateName", authTemplate.TemplateName);
                            command.Parameters.AddWithValue("@authclassid", authTemplate.authclassid);
                            // Explicitly set the parameter type as jsonb
                            command.Parameters["@JsonContent"].NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Jsonb;

                            await command.ExecuteNonQueryAsync();
                        }
                    }
                }
                else
                {
                    using (var connection = new NpgsqlConnection(_connectionString))
                    {
                        await connection.OpenAsync();
                        using (var command = new NpgsqlCommand(
                            "UPDATE authtemplate SET jsoncontent = @JsonContent::jsonb, updatedby = @updatedby, updatedon = @updatedon, templatename = @templateName WHERE id = @id", connection))
                        {
                            command.Parameters.AddWithValue("@JsonContent", authTemplate.JsonContent);
                            command.Parameters.AddWithValue("@updatedby", authTemplate.CreatedBy);
                            command.Parameters.AddWithValue("@updatedon", authTemplate.CreatedOn);
                            command.Parameters.AddWithValue("@templateName", authTemplate.TemplateName);
                            command.Parameters.AddWithValue("@id", authTemplate.Id);

                            // Explicitly set the parameter type for JSONB
                            command.Parameters["@JsonContent"].NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Jsonb;

                            await command.ExecuteNonQueryAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database Error: {ex.Message}");
                throw;
            }
        }

        public async Task<AuthTemplateValidation?> GetByTemplateIdAsync(int templateId)
        {
            const string sql = @"SELECT * FROM AuthTemplateValidation WHERE TemplateId = @TemplateId LIMIT 1";
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@TemplateId", templateId);
            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new AuthTemplateValidation
                {
                    Id = reader.GetInt32(0),
                    TemplateId = reader.GetInt32(1),
                    ValidationJson = reader.GetString(2),
                    CreatedOn = reader.GetDateTime(3),
                    CreatedBy = reader.GetInt32(4),
                    UpdatedOn = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                    UpdatedBy = reader.IsDBNull(6) ? null : reader.GetInt32(6)
                };
            }

            return null;
        }

        public async Task InsertAsync(AuthTemplateValidation entity)
        {
            const string sql = @"
            INSERT INTO AuthTemplateValidation (TemplateId, ValidationJson, CreatedOn, CreatedBy)
            VALUES (@TemplateId, @ValidationJson, @CreatedOn, @CreatedBy)";
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@TemplateId", entity.TemplateId);
            cmd.Parameters.AddWithValue("@ValidationJson", entity.ValidationJson);
            cmd.Parameters.AddWithValue("@CreatedOn", entity.CreatedOn);
            cmd.Parameters.AddWithValue("@CreatedBy", entity.CreatedBy);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdateAsync(AuthTemplateValidation entity)
        {
            const string sql = @"
            UPDATE AuthTemplateValidation 
            SET ValidationJson = @ValidationJson, UpdatedOn = @UpdatedOn, UpdatedBy = @UpdatedBy
            WHERE TemplateId = @TemplateId";
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ValidationJson", entity.ValidationJson);
            cmd.Parameters.AddWithValue("@UpdatedOn", entity.UpdatedOn ?? DateTime.UtcNow);
            cmd.Parameters.AddWithValue("@UpdatedBy", entity.UpdatedBy ?? 0);
            cmd.Parameters.AddWithValue("@TemplateId", entity.TemplateId);
            await cmd.ExecuteNonQueryAsync();
        }

    }
}
