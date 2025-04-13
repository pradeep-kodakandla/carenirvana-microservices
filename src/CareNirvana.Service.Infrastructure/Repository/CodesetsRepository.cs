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
    public class CodesetsRepository : ICodesetsRepository
    {
        private readonly string _connectionString;

        public CodesetsRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        private NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }

        public async Task<IEnumerable<codesets>> GetAllAsync()
        {
            var list = new List<codesets>();

            using var conn = GetConnection();
            await conn.OpenAsync();

            var cmd = new NpgsqlCommand("SELECT * FROM cfgicdcodesmaster", conn);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new codesets
                {
                    Id = reader["icdcodeid"] != DBNull.Value ? Convert.ToInt32(reader["icdcodeid"]) : null,
                    code = reader["code"]?.ToString(),
                    codeDesc = reader["codedescription"]?.ToString(),
                    codeShortDesc = reader["codeshortdescription"]?.ToString(),
                    effectiveDate = reader["effectivedate"] as DateTime?,
                    endDate = reader["enddate"] as DateTime?,
                    severity = reader["severity"]?.ToString(),
                    laterality = reader["laterality"]?.ToString(),
                    activeFlag = reader["activeflag"]?.ToString()
                });
            }

            return list;
        }

        public async Task<codesets?> GetByIdAsync(string id)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            var cmd = new NpgsqlCommand("SELECT * FROM cfgicdcodesmaster WHERE code = @id", conn);
            cmd.Parameters.AddWithValue("id", id);

            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new codesets
                {
                    Id = reader["icdcodeid"] != DBNull.Value ? Convert.ToInt32(reader["icdcodeid"]) : null,
                    code = reader["code"]?.ToString(),
                    codeDesc = reader["codedescription"]?.ToString(),
                    codeShortDesc = reader["codeshortdescription"]?.ToString(),
                    effectiveDate = reader["effectivedate"] as DateTime?,
                    endDate = reader["enddate"] as DateTime?,
                    severity = reader["severity"]?.ToString(),
                    laterality = reader["laterality"]?.ToString(),
                    activeFlag = reader["activeflag"]?.ToString()
                };
            }

            return null;
        }

        public async Task<codesets> InsertAsync(codesets entity)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            var cmd = new NpgsqlCommand(@"
            INSERT INTO cfgicdcodesmaster 
            (code, codedescription, codeshortdescription, effectivedate, enddate, severity, laterality, activeflag) 
            VALUES 
            (@code, @codedesc, @codeshortdesc, @effectivedate, @enddate, @severity, @laterality, @activeflag)
            RETURNING id", conn);

            cmd.Parameters.AddWithValue("code", entity.code ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("codedescription", entity.codeDesc ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("codeshortdescription", entity.codeShortDesc ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("effectivedate", (object?)entity.effectiveDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("enddate", (object?)entity.endDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("severity", entity.severity ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("laterality", entity.laterality ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("activeflag", entity.activeFlag ?? (object)DBNull.Value);

            entity.Id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            return entity;
        }

        public async Task<codesets> UpdateAsync(codesets entity)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            var cmd = new NpgsqlCommand(@"
            UPDATE cfgicdcodesmaster SET 
                code = @code,
                codedescription = @codedesc,
                codeshortdescription = @codeshortdesc,
                effectivedate = @effectivedate,
                enddate = @enddate,
                severity = @severity,
                laterality = @laterality,
                activeflag = @activeflag
            WHERE icdcodeid = @id", conn);

            cmd.Parameters.AddWithValue("id", entity.Id ?? throw new ArgumentNullException(nameof(entity.Id)));
            cmd.Parameters.AddWithValue("code", entity.code ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("codedescription", entity.codeDesc ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("codeshortdescription", entity.codeShortDesc ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("effectivedate", (object?)entity.effectiveDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("enddate", (object?)entity.endDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("severity", entity.severity ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("laterality", entity.laterality ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("activeflag", entity.activeFlag ?? (object)DBNull.Value);

            await cmd.ExecuteNonQueryAsync();
            return entity;
        }
    }
}
