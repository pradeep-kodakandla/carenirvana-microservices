using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CareNirvana.Service.Infrastructure.Repository
{
    public class CfgvalidationRepository : ICfgvalidationRepository
    {
        private readonly string _connectionString;

        public CfgvalidationRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        private NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }

        public async Task<IEnumerable<Cfgvalidation>> GetAllAsync(int moduleId)
        {
            var list = new List<Cfgvalidation>();

            await using var conn = GetConnection();
            await conn.OpenAsync();

            const string sql = @"
                SELECT validationid, validationjson, validationname, moduleid, activeflag,
                       createdon, createdby, updatedon, updatedby, deletedon, deletedby
                FROM public.Cfgvalidation
                WHERE moduleid = @moduleid
                  AND (deletedon IS NULL)
                ORDER BY validationid;";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("moduleid", moduleId);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(Map(reader));
            }

            return list;
        }

        public async Task<Cfgvalidation?> GetByIdAsync(int validationId)
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();

            const string sql = @"
                SELECT validationid, validationjson, validationname, moduleid, activeflag,
                       createdon, createdby, updatedon, updatedby, deletedon, deletedby
                FROM public.Cfgvalidation
                WHERE validationid = @id
                  AND (deletedon IS NULL)
                LIMIT 1;";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", validationId);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
                return Map(reader);

            return null;
        }

        public async Task<Cfgvalidation> InsertAsync(Cfgvalidation entity)
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();

            // If validationId is provided, insert with id.
            // If DB generates validationid (IDENTITY/sequence), send entity.validationId = null and it will be omitted.
            string sql;
            if (entity.validationId.HasValue)
            {
                sql = @"
                    INSERT INTO public.Cfgvalidation
                        (validationid, validationjson, validationname, moduleid, activeflag, createdon, createdby)
                    VALUES
                        (@validationid, @validationjson::jsonb, @validationname, @moduleid, @activeflag, CURRENT_TIMESTAMP, @createdby)
                    RETURNING validationid;";
            }
            else
            {
                sql = @"
                    INSERT INTO public.Cfgvalidation
                        (validationjson, validationname, moduleid, activeflag, createdon, createdby)
                    VALUES
                        (@validationjson::jsonb,  @validationname, @moduleid, @activeflag, CURRENT_TIMESTAMP, @createdby)
                    RETURNING validationid;";
            }

            await using var cmd = new NpgsqlCommand(sql, conn);

            if (entity.validationId.HasValue)
                cmd.Parameters.AddWithValue("validationid", entity.validationId.Value);

            cmd.Parameters.AddWithValue("validationjson", entity.validationJson ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("validationname", entity.validationName ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("moduleid", entity.moduleId);
            cmd.Parameters.AddWithValue("activeflag", entity.activeFlag);
            cmd.Parameters.AddWithValue("createdby", (object?)entity.createdBy ?? DBNull.Value);

            entity.validationId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            return entity;
        }

        public async Task<Cfgvalidation> UpdateAsync(Cfgvalidation entity)
        {
            if (!entity.validationId.HasValue)
                throw new ArgumentNullException(nameof(entity.validationId));

            await using var conn = GetConnection();
            await conn.OpenAsync();

            const string sql = @"
                UPDATE public.Cfgvalidation
                SET
                    validationjson = @validationjson::jsonb,
                    moduleid = @moduleid,
                    activeflag = @activeflag,
                    updatedon = CURRENT_TIMESTAMP,
                    updatedby = @updatedby
                WHERE validationid = @id
                  AND (deletedon IS NULL);";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", entity.validationId.Value);
            cmd.Parameters.AddWithValue("validationjson", entity.validationJson ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("moduleid", entity.moduleId);
            cmd.Parameters.AddWithValue("activeflag", entity.activeFlag);
            cmd.Parameters.AddWithValue("updatedby", (object?)entity.updatedBy ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync();
            return entity;
        }

        public async Task<bool> DeleteAsync(int validationId, int deletedBy)
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();

            // Soft delete
            const string sql = @"
                UPDATE public.Cfgvalidation
                SET
                    activeflag = false,
                    deletedon = CURRENT_TIMESTAMP,
                    deletedby = @deletedby
                WHERE validationid = @id
                  AND (deletedon IS NULL);";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", validationId);
            cmd.Parameters.AddWithValue("deletedby", deletedBy);

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        private static Cfgvalidation Map(NpgsqlDataReader reader)
        {
            return new Cfgvalidation
            {
                validationId = reader["validationid"] != DBNull.Value ? Convert.ToInt32(reader["validationid"]) : null,
                validationJson = reader["validationjson"] == DBNull.Value ? null : reader["validationjson"].ToString(),
                validationName = reader["validationname"] == DBNull.Value ? null : reader["validationname"].ToString(),
                moduleId = Convert.ToInt32(reader["moduleid"]),
                activeFlag = reader["activeflag"] != DBNull.Value && Convert.ToBoolean(reader["activeflag"]),

                createdOn = reader["createdon"] as DateTime?,
                createdBy = reader["createdby"] == DBNull.Value ? null : (int?)Convert.ToInt32(reader["createdby"]),

                updatedOn = reader["updatedon"] as DateTime?,
                updatedBy = reader["updatedby"] == DBNull.Value ? null : (int?)Convert.ToInt32(reader["updatedby"]),

                deletedOn = reader["deletedon"] as DateTime?,
                deletedBy = reader["deletedby"] == DBNull.Value ? null : (int?)Convert.ToInt32(reader["deletedby"])
            };
        }

        public async Task<string?> GetPrimaryTemplateJsonAsync(int moduleId)
        {
            // UM => cfgauthtemplate, else cfgcasetemplate
            var tableName = moduleId == 2 ? "public.cfgauthtemplate" : "public.cfgcasetemplate";

            using var conn = GetConnection();
            await conn.OpenAsync();

            // jsoncontent column, isprimary=1 (true)
            var sql = $@"
                SELECT jsoncontent
                FROM {tableName}
                WHERE isprimary = true
                LIMIT 1;";

            using var cmd = new NpgsqlCommand(sql, conn);
            var result = await cmd.ExecuteScalarAsync();

            return result == null || result == DBNull.Value ? null : result.ToString();
        }
    }
}
