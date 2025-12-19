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


        public async Task<List<AuthTemplate>> GetAllAsync(int classId, string module)
        {
            var templates = new List<AuthTemplate>();

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = string.Empty;
                if (module == "UM")
                {
                    query = "SELECT authtemplateid, authtemplatename, cat.createdon, cat.createdby, su.username, authclassid FROM cfgauthtemplate cat join securityuser su on su.userid=cat.createdby";
                    if (classId > 0)
                    {
                        query += " WHERE authclassid = @classid";
                    }
                    query += " ORDER BY COALESCE(cat.updatedon, cat.createdon) DESC";
                }
                else if (module == "AG")
                {
                    query = "SELECT casetemplateid, casetemplatename, cct.createdon, cct.createdby, su.username FROM cfgcasetemplate cct join securityuser su on su.userid=cct.createdby ORDER BY COALESCE(cct.updatedon, cct.createdon) DESC";
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
                                CreatedByUser = reader.GetString(4),
                                authclassid = module == "UM" && !reader.IsDBNull(5) ? reader.GetInt32(5) : (int?)null
                            });
                        }
                    }
                }

            }

            return templates;
        }


        public async Task<List<AuthTemplate>> GetAuthTemplate(int id, string module)
        {
            var templates = new List<AuthTemplate>();
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                if (module == "UM")
                {
                    // Add WHERE clause with parameterized query
                    using (var command = new NpgsqlCommand("SELECT authtemplateid, authtemplatename,jsoncontent, createdon, createdby, authclassid FROM cfgauthtemplate WHERE authtemplateid = @id", connection))
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
                else if (module == "AG")
                {
                    using (var command = new NpgsqlCommand("SELECT casetemplateid, casetemplatename, jsoncontent, createdon, createdby FROM cfgcasetemplate WHERE casetemplateid = @id", connection))
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
                                    authclassid = null
                                });
                            }
                        }
                    }
                }
            }
            return templates;
        }

        public async Task SaveAsync(AuthTemplate authTemplate, string module)
        {
            try
            {

                Console.WriteLine($"Module: {module}");
                if (module == "UM")
                {
                    if (authTemplate.Id == 0)
                    {
                        using (var connection = new NpgsqlConnection(_connectionString))
                        {
                            await connection.OpenAsync();
                            using (var command = new NpgsqlCommand(
                                "INSERT INTO cfgauthtemplate (jsoncontent, createdby, createdon, authtemplatename, authclassid, enrollmenthierarhcyid) VALUES (@JsonContent::jsonb, @createdby,@createdon, @templateName, @authclassid, @enrollmenthierarhcyid)", connection))
                            {
                                // Ensure the data is inserted as a JSONB array
                                command.Parameters.AddWithValue("@JsonContent", authTemplate.JsonContent);
                                command.Parameters.AddWithValue("@createdby", authTemplate.CreatedBy);
                                command.Parameters.AddWithValue("@createdon", authTemplate.CreatedOn);
                                command.Parameters.AddWithValue("@templateName", authTemplate.TemplateName);
                                command.Parameters.AddWithValue("@authclassid", authTemplate.authclassid);
                                command.Parameters.AddWithValue("@enrollmenthierarhcyid", authTemplate.EnrollmentHierarchyId ?? (object)DBNull.Value);
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
                                "UPDATE cfgauthtemplate SET jsoncontent = @JsonContent::jsonb, updatedby = @updatedby, updatedon = @updatedon, authtemplatename = @templateName WHERE authtemplateid = @id", connection))
                            {
                                command.Parameters.AddWithValue("@JsonContent", authTemplate.JsonContent);
                                command.Parameters.AddWithValue("@updatedby", authTemplate.CreatedBy);
                                command.Parameters.AddWithValue("@updatedon", authTemplate.CreatedOn);
                                command.Parameters.AddWithValue("@templateName", authTemplate.TemplateName);
                                command.Parameters.Add("@id", NpgsqlTypes.NpgsqlDbType.Integer).Value = authTemplate.Id!.Value;
                                // Explicitly set the parameter type for JSONB
                                command.Parameters["@JsonContent"].NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Jsonb;

                                await command.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }
                else if (module == "AG")
                {
                    if (authTemplate.Id == 0)
                    {
                        using (var connection = new NpgsqlConnection(_connectionString))
                        {
                            await connection.OpenAsync();
                            using (var command = new NpgsqlCommand(
                                "INSERT INTO cfgcasetemplate (jsoncontent, createdby, createdon, casetemplatename, enrollmenthierarhcyid) VALUES (@JsonContent::jsonb, @createdby,@createdon, @templateName, @enrollmenthierarhcyid)", connection))
                            {
                                // Ensure the data is inserted as a JSONB array
                                command.Parameters.AddWithValue("@JsonContent", authTemplate.JsonContent);
                                command.Parameters.AddWithValue("@createdby", authTemplate.CreatedBy);
                                command.Parameters.AddWithValue("@createdon", authTemplate.CreatedOn);
                                command.Parameters.AddWithValue("@templateName", authTemplate.TemplateName);
                                command.Parameters.AddWithValue("@enrollmenthierarhcyid", authTemplate.EnrollmentHierarchyId ?? (object)DBNull.Value);
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
                                "UPDATE cfgcasetemplate SET jsoncontent = @JsonContent::jsonb, updatedby = @updatedby, updatedon = @updatedon, casetemplatename = @templateName WHERE casetemplateid = @id", connection))
                            {
                                command.Parameters.AddWithValue("@JsonContent", authTemplate.JsonContent);
                                command.Parameters.AddWithValue("@updatedby", authTemplate.CreatedBy);
                                command.Parameters.AddWithValue("@updatedon", authTemplate.CreatedOn);
                                command.Parameters.AddWithValue("@templateName", authTemplate.TemplateName);
                                command.Parameters.Add("@id", NpgsqlTypes.NpgsqlDbType.Integer).Value = authTemplate.Id!.Value;

                                // Explicitly set the parameter type for JSONB
                                command.Parameters["@JsonContent"].NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Jsonb;

                                await command.ExecuteNonQueryAsync();
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
            cmd.Parameters.AddWithValue("@ValidationJson", NpgsqlTypes.NpgsqlDbType.Jsonb, entity.ValidationJson);
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
            cmd.Parameters.AddWithValue("@ValidationJson", NpgsqlTypes.NpgsqlDbType.Jsonb, entity.ValidationJson);
            cmd.Parameters.AddWithValue("@UpdatedOn", entity.UpdatedOn ?? DateTime.UtcNow);
            cmd.Parameters.AddWithValue("@UpdatedBy", entity.UpdatedBy ?? 0);
            cmd.Parameters.AddWithValue("@TemplateId", entity.TemplateId);
            await cmd.ExecuteNonQueryAsync();
        }

    }
}
