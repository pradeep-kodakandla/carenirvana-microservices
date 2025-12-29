using CareNirvana.Service.Application.Interfaces;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CareNirvana.Service.Infrastructure.Repository
{
    public class ConfigAdminRepository : IConfigAdminRepository
    {
        private readonly string _connectionString;

        public ConfigAdminRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        private NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }

        //public async Task<JsonElement?> GetSectionData(string module, string? section)
        //{
        //    module = module.ToUpper();
        //    await using var conn = GetConnection();
        //    await conn.OpenAsync();


        //    const string query = @"
        //          SELECT
        //            CASE
        //              WHEN @section IS NULL OR @section = '' THEN jsoncontent
        //              ELSE jsoncontent -> @section
        //            END AS section_data
        //          FROM cfgadmindata
        //          WHERE UPPER(module) = @module
        //            AND (
        //                  @section IS NULL OR @section = ''
        //                  OR jsoncontent ? @section
        //                )
        //          LIMIT 1;
        //        ";
        //    await using var cmd = new NpgsqlCommand(query, conn);
        //    cmd.Parameters.AddWithValue("module", module);
        //    cmd.Parameters.AddWithValue("section", section);

        //    await using var reader = await cmd.ExecuteReaderAsync();
        //    if (!reader.HasRows)
        //        return null;

        //    await reader.ReadAsync();
        //    return JsonDocument.Parse(reader["section_data"].ToString()).RootElement;
        //}

        public async Task<JsonElement?> GetSectionData(string module, string? section)
        {
            module = module.ToUpperInvariant();

            await using var conn = GetConnection();
            await conn.OpenAsync();

            const string query = @"
                  SELECT
                    CASE
                      WHEN @section IS NULL OR @section = '' THEN jsoncontent
                      ELSE jsoncontent -> @section
                    END AS section_data
                  FROM cfgadmindata
                  WHERE UPPER(module) = @module
                    AND (
                          @section IS NULL OR @section = ''
                          OR jsoncontent ? @section
                        )
                  LIMIT 1;
                ";

            await using var cmd = new NpgsqlCommand(query, conn);

            cmd.Parameters.AddWithValue("module", module);

            // IMPORTANT: send NULL properly
            var p = cmd.Parameters.Add("section", NpgsqlTypes.NpgsqlDbType.Text);
            p.Value = (object?)section ?? DBNull.Value;

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            var raw = reader["section_data"]?.ToString();
            if (string.IsNullOrWhiteSpace(raw)) return null;

            return JsonDocument.Parse(raw).RootElement;
        }

        public async Task<JsonElement> AddEntry(string module, string section, JsonElement newEntry)
        {
            module = module.ToUpper();
            await using var conn = GetConnection();
            await conn.OpenAsync();

            var query = @"
        UPDATE cfgadmindata 
        SET jsoncontent = jsonb_set(
            jsoncontent,
            @path::text[],
            COALESCE(jsoncontent->@section, '[]'::jsonb) || @value::jsonb,
            true
        )
        WHERE UPPER(module) = @module 
        RETURNING jsoncontent";

            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("path", new[] { section }); // e.g., {"qualityindicator"}
            cmd.Parameters.AddWithValue("section", section); // e.g., "qualityindicator"
            cmd.Parameters.AddWithValue("value", newEntry.ToString()); // The new entry
            cmd.Parameters.AddWithValue("module", module); // e.g., "UM"

            var result = await cmd.ExecuteScalarAsync();
            return JsonDocument.Parse(result.ToString()).RootElement;
        }

        public async Task<JsonElement?> UpdateEntry(string module, string section, string id, JsonElement updatedEntry)
        {
            module = module.ToUpper();
            await using var conn = GetConnection();
            await conn.OpenAsync();
            Console.WriteLine($"updatedEntry: {updatedEntry.ToString()}");
            var query = @"
        UPDATE cfgadmindata 
        SET jsoncontent = jsonb_set(
            jsoncontent,
            @section_path::text[],
            (
                SELECT jsonb_agg(
                    CASE WHEN elem->>'id' = @id THEN @value::jsonb
                         ELSE elem
                    END
                )
                FROM jsonb_array_elements(jsoncontent->@section) AS elem
            ),
            false
        )
        WHERE UPPER(module) = @module 
        RETURNING jsoncontent->@section";

            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("section_path", new[] { section }); // e.g., {"qualityindicator"}
            cmd.Parameters.AddWithValue("section", section); // e.g., "qualityindicator"
            cmd.Parameters.AddWithValue("id", id); // e.g., "2"
            cmd.Parameters.AddWithValue("value", updatedEntry.ToString()); // The updated JSON object as a string
            cmd.Parameters.AddWithValue("module", module); // e.g., "UM"

            var result = await cmd.ExecuteScalarAsync();
            return result == null ? null : JsonDocument.Parse(result.ToString()).RootElement;
        }


        public async Task<bool> DeleteEntry(string module, string section, string id, JsonElement deleteInfo)
        {
            module = module.ToUpper();
            await using var conn = GetConnection();
            await conn.OpenAsync();

            var query = @"
        UPDATE cfgadmindata 
        SET jsoncontent = jsonb_set(
            jsoncontent,
            @path::text[],
            (
                SELECT jsonb_agg(
                    CASE WHEN elem->>'id' = @id THEN elem || @value::jsonb
                         ELSE elem
                    END
                )
                FROM jsonb_array_elements(jsoncontent->@section) AS elem
            ),
            false
        )
        WHERE UPPER(module) = @module 
        RETURNING jsoncontent";

            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("path", new[] { section }); // e.g., {"qualityindicator"}
            cmd.Parameters.AddWithValue("section", section); // e.g., "qualityindicator"
            cmd.Parameters.AddWithValue("id", id); // e.g., "2"
            cmd.Parameters.AddWithValue("value", deleteInfo.ToString()); // e.g., {"deletedBy": "user", "deletedOn": "timestamp"}
            cmd.Parameters.AddWithValue("module", module); // e.g., "UM"

            var rowsAffected = await cmd.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }
    }
}
