using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareNirvana.Service.Infrastructure.Repository
{
    public class MemberEnrollmentRepository : IMemberEnrollmentRepository
    {
        private readonly string _connectionString;

        public MemberEnrollmentRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<List<MemberEnrollment>> GetMemberEnrollment(int memberdetailsId)
        {
            var results = new List<MemberEnrollment>();

            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            const string sql = @"
        SELECT
          memberenrollmentid,
          memberdetailsid,
          startdate,
          enddate,
          -- normalize to boolean regardless of how status is stored in the view/table
          CASE
            WHEN status::text IN ('true','t','1','TRUE','True') THEN TRUE
            WHEN status::text IN ('false','f','0','FALSE','False') THEN FALSE
            ELSE FALSE
          END AS status,
          hierarchy_path,
          level_map::text AS level_map,
          levels::text     AS levels
        FROM vw_member_enrollment_hierarchy_json
        WHERE memberdetailsid = @memberdetailsid
        ORDER BY memberenrollmentid;";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@memberdetailsid", memberdetailsId);

            await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
            while (await reader.ReadAsync())
            {
                var item = new MemberEnrollment
                {
                    MemberEnrollmentId = Convert.ToInt32(reader["memberenrollmentid"]),
                    MemberDetailsId = Convert.ToInt32(reader["memberdetailsid"]),
                    StartDate = reader["startdate"] is DBNull ? DateTime.MinValue : (DateTime)reader["startdate"],
                    EndDate = reader["enddate"] is DBNull ? DateTime.MinValue : (DateTime)reader["enddate"],
                    Status = reader["status"] is bool b ? b :
                                         string.Equals(reader["status"]?.ToString(), "true", StringComparison.OrdinalIgnoreCase),
                    HierarchyPath = reader["hierarchy_path"]?.ToString() ?? string.Empty,
                    LevelMap = reader["level_map"]?.ToString() ?? "{}",   // json text
                    Levels = reader["levels"]?.ToString() ?? "[]"     // json text
                };

                results.Add(item);
            }

            return results;
        }
    }
}
