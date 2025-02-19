using CareNirvana.Service.Application.Interfaces;
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

        public async Task<JsonElement?> GetSectionData(string module, string section)
        {
            module = module.ToUpper();
            await using var conn = GetConnection();
            await conn.OpenAsync();

            var query = "SELECT jsoncontent -> @section AS section_data FROM cfgadmindata WHERE UPPER(module) = @module AND jsoncontent ? @section";
            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("module", module);
            cmd.Parameters.AddWithValue("section", section);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!reader.HasRows)
                return null;

            await reader.ReadAsync();
            return JsonDocument.Parse(reader["section_data"].ToString()).RootElement;
        }

        public async Task<JsonElement> AddEntry(string module, string section, JsonElement newEntry)
        {
            module = module.ToUpper();
            await using var conn = GetConnection();
            await conn.OpenAsync();

            var query = "UPDATE cfgadmindata SET jsoncontent = jsonb_set(jsoncontent, @path, @value::jsonb, true) WHERE UPPER(module) = @module RETURNING jsoncontent";
            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("path", "{" + section + "}");
            cmd.Parameters.AddWithValue("value", newEntry.ToString());
            cmd.Parameters.AddWithValue("module", module);

            var result = await cmd.ExecuteScalarAsync();
            return JsonDocument.Parse(result.ToString()).RootElement;
        }

        public async Task<JsonElement?> UpdateEntry(string module, string section, string id, JsonElement updatedEntry)
        {
            module = module.ToUpper();
            await using var conn = GetConnection();
            await conn.OpenAsync();

            var query = "UPDATE cfgadmindata SET jsoncontent = jsonb_set(jsoncontent, @path, @value::jsonb, false) WHERE UPPER(module) = @module RETURNING jsoncontent";
            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("path", "{" + section + "}");
            cmd.Parameters.AddWithValue("value", updatedEntry.ToString());
            cmd.Parameters.AddWithValue("module", module);

            var result = await cmd.ExecuteScalarAsync();
            return result == null ? null : JsonDocument.Parse(result.ToString()).RootElement;
        }

        public async Task<bool> DeleteEntry(string module, string section, string id)
        {
            module = module.ToUpper();
            await using var conn = GetConnection();
            await conn.OpenAsync();

            var query = "DELETE FROM cfgadmindata WHERE id = @id AND UPPER(module) = @module";
            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("module", module);

            var rowsAffected = await cmd.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }
    }
}
