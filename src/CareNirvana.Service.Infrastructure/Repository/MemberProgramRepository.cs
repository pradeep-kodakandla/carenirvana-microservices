using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Data;

namespace CareNirvana.Service.Infrastructure.Repository
{
    public class MemberProgramRepository : IMemberProgramRepository
    {
        private readonly string _connectionString;

        public MemberProgramRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }
        public async Task<int> InsertMemberProgramAsync(MemberProgram mp)
        {
            const string sql = @"
                INSERT INTO memberprogram
                (memberdetailsid, programid, memberenrollmentid, programstatusid, programstatusreasonid, programreferralsourceid,
                 assignedto, startdate, enddate, activeflag, createdon, createdby, updatedon, updatedby)
                VALUES
                (@memberdetailsid, @programid, @memberenrollmentid, @programstatusid, @programstatusreasonid, @programreferralsourceid,
                 @assignedto, @startdate, @enddate, COALESCE(@activeflag, TRUE), COALESCE(@createdon, NOW()), @createdby, @updatedon, @updatedby)
                RETURNING memberprogramid;";

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@memberdetailsid", mp.MemberDetailsId);
            cmd.Parameters.AddWithValue("@programid", mp.ProgramId);
            cmd.Parameters.AddWithValue("@memberenrollmentid", (object?)mp.MemberEnrollmentId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@programstatusid", mp.ProgramStatusId);
            cmd.Parameters.AddWithValue("@programstatusreasonid", (object?)mp.ProgramStatusReasonId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@programreferralsourceid", (object?)mp.ProgramReferralSourceId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@assignedto", (object?)mp.AssignedTo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@startdate", mp.StartDate);
            cmd.Parameters.AddWithValue("@enddate", (object?)mp.EndDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@activeflag", (object?)mp.ActiveFlag ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@createdon", (object?)mp.CreatedOn == default ? DBNull.Value : mp.CreatedOn);
            cmd.Parameters.AddWithValue("@createdby", mp.CreatedBy);
            cmd.Parameters.AddWithValue("@updatedon", (object?)mp.UpdatedOn ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@updatedby", (object?)mp.UpdatedBy ?? DBNull.Value);

            var id = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(id);
        }

        public async Task<int> UpdateMemberProgramAsync(MemberProgram mp)
        {
            const string sql = @"
UPDATE memberprogram
SET
    memberdetailsid          = @memberdetailsid,
    programid                = @programid,
    memberenrollmentid       = @memberenrollmentid,
    programstatusid          = @programstatusid,
    programstatusreasonid    = @programstatusreasonid,
    programreferralsourceid  = @programreferralsourceid,
    assignedto               = @assignedto,
    startdate                = @startdate,
    enddate                  = @enddate,
    activeflag               = COALESCE(@activeflag, activeflag),
    updatedon                = COALESCE(@updatedon, NOW()),
    updatedby                = @updatedby
WHERE memberprogramid = @id
  AND deletedon IS NULL;";

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", mp.MemberProgramId);
            cmd.Parameters.AddWithValue("@memberdetailsid", mp.MemberDetailsId);
            cmd.Parameters.AddWithValue("@programid", mp.ProgramId);
            cmd.Parameters.AddWithValue("@memberenrollmentid", (object?)mp.MemberEnrollmentId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@programstatusid", mp.ProgramStatusId);
            cmd.Parameters.AddWithValue("@programstatusreasonid", (object?)mp.ProgramStatusReasonId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@programreferralsourceid", (object?)mp.ProgramReferralSourceId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@assignedto", (object?)mp.AssignedTo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@startdate", mp.StartDate);
            cmd.Parameters.AddWithValue("@enddate", (object?)mp.EndDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@activeflag", (object?)mp.ActiveFlag ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@updatedon", (object?)mp.UpdatedOn ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@updatedby", (object?)mp.UpdatedBy ?? DBNull.Value);

            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task<int> SoftDeleteMemberProgramAsync(int memberProgramId, int deletedBy)
        {
            const string sql = @"
UPDATE memberprogram
SET deletedon = NOW(),
    deletedby = @deletedby
WHERE memberprogramid = @id
  AND deletedon IS NULL;";

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", memberProgramId);
            cmd.Parameters.AddWithValue("@deletedby", deletedBy);

            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task<MemberProgram?> GetMemberProgramByIdAsync(int id)
        {
            const string sql = @"
SELECT memberprogramid, memberdetailsid, programid, memberenrollmentid,
       programstatusid, programstatusreasonid, programreferralsourceid,
       assignedto, startdate, enddate, activeflag,
       createdon, createdby, updatedon, updatedby, deletedon, deletedby
FROM memberprogram
WHERE memberprogramid = @id;";

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow);
            if (await reader.ReadAsync())
            {
                return Map(reader);
            }
            return null;
        }

        public async Task<(List<MemberProgram> Items, int Total)>
            GetMemberProgramsForMemberAsync(int memberDetailsId, int page = 1, int pageSize = 25, bool includeDeleted = false)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 25;

            var list = new List<MemberProgram>();
            var total = 0;
            var filter = includeDeleted ? "" : "AND mp.deletedon IS NULL";

            var sql = $@"
SELECT
    mp.memberprogramid, mp.memberdetailsid, mp.programid, mp.memberenrollmentid,
    mp.programstatusid, mp.programstatusreasonid, mp.programreferralsourceid,
    mp.assignedto, mp.startdate, mp.enddate, mp.activeflag,
    mp.createdon, mp.createdby, mp.updatedon, mp.updatedby, mp.deletedon, mp.deletedby,
    COUNT(*) OVER() AS total_count
FROM memberprogram mp
WHERE mp.memberdetailsid = @memberdetailsid
  {filter}
ORDER BY COALESCE(mp.updatedon, mp.createdon) DESC, mp.memberprogramid DESC
OFFSET @offset LIMIT @limit;";

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@memberdetailsid", memberDetailsId);
            cmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
            cmd.Parameters.AddWithValue("@limit", pageSize);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (total == 0 && !reader.IsDBNull(reader.GetOrdinal("total_count")))
                    total = reader.GetInt32(reader.GetOrdinal("total_count"));

                list.Add(Map(reader));
            }

            return (list, total);
        }

        public async Task<List<MemberProgram>> GetActiveMemberProgramsForMemberAsync(int memberDetailsId)
        {
            const string sql = @"
SELECT memberprogramid, memberdetailsid, programid, memberenrollmentid,
       programstatusid, programstatusreasonid, programreferralsourceid,
       assignedto, startdate, enddate, activeflag,
       createdon, createdby, updatedon, updatedby, deletedon, deletedby
FROM memberprogram
WHERE memberdetailsid = @memberdetailsid
  AND COALESCE(activeflag, TRUE) = TRUE
  AND deletedon IS NULL
ORDER BY COALESCE(updatedon, createdon) DESC;";

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@memberdetailsid", memberDetailsId);

            var list = new List<MemberProgram>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(Map(reader));
            }
            return list;
        }

        private static MemberProgram Map(NpgsqlDataReader reader)
        {
            return new MemberProgram
            {
                MemberProgramId = reader.GetInt32(reader.GetOrdinal("memberprogramid")),
                MemberDetailsId = reader.GetInt32(reader.GetOrdinal("memberdetailsid")),
                ProgramId = reader.GetInt32(reader.GetOrdinal("programid")),
                MemberEnrollmentId = reader.IsDBNull(reader.GetOrdinal("memberenrollmentid")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("memberenrollmentid")),
                ProgramStatusId = reader.GetInt32(reader.GetOrdinal("programstatusid")),
                ProgramStatusReasonId = reader.IsDBNull(reader.GetOrdinal("programstatusreasonid")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("programstatusreasonid")),
                ProgramReferralSourceId = reader.IsDBNull(reader.GetOrdinal("programreferralsourceid")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("programreferralsourceid")),
                AssignedTo = reader.IsDBNull(reader.GetOrdinal("assignedto")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("assignedto")),
                StartDate = reader.GetDateTime(reader.GetOrdinal("startdate")),
                EndDate = reader.IsDBNull(reader.GetOrdinal("enddate")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("enddate")),
                ActiveFlag = reader.IsDBNull(reader.GetOrdinal("activeflag")) ? (bool?)null : reader.GetBoolean(reader.GetOrdinal("activeflag")),
                CreatedOn = reader.GetDateTime(reader.GetOrdinal("createdon")),
                CreatedBy = reader.GetInt32(reader.GetOrdinal("createdby")),
                UpdatedOn = reader.IsDBNull(reader.GetOrdinal("updatedon")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("updatedon")),
                UpdatedBy = reader.IsDBNull(reader.GetOrdinal("updatedby")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("updatedby")),
                DeletedOn = reader.IsDBNull(reader.GetOrdinal("deletedon")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("deletedon")),
                DeletedBy = reader.IsDBNull(reader.GetOrdinal("deletedby")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("deletedby"))
            };
        }
    }
}
