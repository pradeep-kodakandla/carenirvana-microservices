using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace CareNirvana.Service.Infrastructure.Repository
{
    /// <summary>
    /// Repository updated to use public.membernote (replacing legacy memberhealthnotes).
    /// Method names/signatures are preserved to avoid breaking callers.
    /// - "memberId" parameters now represent MemberDetailsId.
    /// - BIT(1) columns are handled via CASE/B'0'/B'1' on write, and boolean aliases on read.
    /// </summary>
    public class MemberNotesRepository : IMemberNotes
    {
        private readonly string _connectionString;

        public MemberNotesRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<long> InsertMemberHealthNoteAsync(MemberHealthNote note)
        {
            const string sql = @"
INSERT INTO membernote
(
    memberdetailsid, notetypeid, membernotes, enteredtimestamp,
    isalert, isexternal, displayinmemberportal, activeflag,
    createdon, createdby, updatedon, updatedby, alertenddatetime,
    memberprogramid, memberactivityid
)
VALUES
(
    @memberdetailsid, @notetypeid, @membernotes, @enteredts,
    CASE WHEN @isalert THEN B'1' ELSE B'0' END,
    CASE WHEN @isexternal THEN B'1' ELSE B'0' END,
    CASE WHEN @displayportal THEN B'1' ELSE B'0' END,
    COALESCE(@activeflag, TRUE),
    COALESCE(@createdon, NOW()), @createdby, @updatedon, @updatedby, @alertend,
    @memberprogramid, @memberactivityid
)
RETURNING membernoteid;";

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@memberdetailsid", (object?)note.MemberDetailsId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@notetypeid", (object?)note.NoteTypeId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@membernotes", note.Notes ?? string.Empty);
            cmd.Parameters.AddWithValue("@enteredts", (object?)note.EnteredTimestamp ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@isalert", note.IsAlert);
            cmd.Parameters.AddWithValue("@isexternal", note.IsExternal);
            cmd.Parameters.AddWithValue("@displayportal", note.DisplayInMemberPortal);
            cmd.Parameters.AddWithValue("@activeflag", note.ActiveFlag);
            cmd.Parameters.AddWithValue("@createdon", (object?)note.CreatedOn == null || note.CreatedOn == default ? DBNull.Value : note.CreatedOn);
            cmd.Parameters.AddWithValue("@createdby", (object?)note.CreatedBy ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@updatedon", (object?)note.UpdatedOn ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@updatedby", (object?)note.UpdatedBy ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@alertend", (object?)note.AlertEndDateTime ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@memberprogramid", (object?)note.MemberProgramId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@memberactivityid", (object?)note.MemberActivityId ?? DBNull.Value);

            var id = await cmd.ExecuteScalarAsync();
            note.Id = Convert.ToInt64(id);
            return note.Id;
        }

        public async Task<int> UpdateMemberHealthNoteAsync(MemberHealthNote note)
        {
            const string sql = @"
UPDATE membernote
SET
    notetypeid = @notetypeid,
    membernotes = @membernotes,
    enteredtimestamp = @enteredts,
    isalert = CASE WHEN @isalert THEN B'1' ELSE B'0' END,
    isexternal = CASE WHEN @isexternal THEN B'1' ELSE B'0' END,
    displayinmemberportal = CASE WHEN @displayportal THEN B'1' ELSE B'0' END,
    activeflag = COALESCE(@activeflag, TRUE),
    updatedon = COALESCE(@updatedon, NOW()),
    updatedby = @updatedby,
    alertenddatetime = @alertend,
    memberprogramid = @memberprogramid,
    memberactivityid = @memberactivityid
WHERE membernoteid = @id
  AND deletedon IS NULL;";

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", note.MemberHealthNotesId);
            cmd.Parameters.AddWithValue("@notetypeid", (object?)note.NoteTypeId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@membernotes", note.Notes ?? string.Empty);
            cmd.Parameters.AddWithValue("@enteredts", (object?)note.EnteredTimestamp ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@isalert", note.IsAlert);
            cmd.Parameters.AddWithValue("@isexternal", note.IsExternal);
            cmd.Parameters.AddWithValue("@displayportal", note.DisplayInMemberPortal);
            cmd.Parameters.AddWithValue("@activeflag", note.ActiveFlag);
            cmd.Parameters.AddWithValue("@updatedon", (object?)note.UpdatedOn ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@updatedby", (object?)note.UpdatedBy ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@alertend", (object?)note.AlertEndDateTime ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@memberprogramid", (object?)note.MemberProgramId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@memberactivityid", (object?)note.MemberActivityId ?? DBNull.Value);

            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task<int> SoftDeleteMemberHealthNoteAsync(long memberHealthNotesId, int deletedBy)
        {
            const string sql = @"
UPDATE membernote
SET deletedon = NOW(),
    deletedby = @deletedby
WHERE membernoteid = @id
  AND deletedon IS NULL;";

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", memberHealthNotesId);
            cmd.Parameters.AddWithValue("@deletedby", deletedBy);

            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task<MemberHealthNote?> GetMemberHealthNoteByIdAsync(long id)
        {
            const string sql = @"
SELECT
    m.membernoteid,
    m.memberdetailsid,
    m.notetypeid,
    m.membernotes,
    m.enteredtimestamp,
    (m.isalert = B'1') as isalert_bool,
    (m.isexternal = B'1') as isexternal_bool,
    (m.displayinmemberportal = B'1') as displayportal_bool,
    m.activeflag,
    m.createdon, m.createdby, m.updatedon, m.updatedby, m.deletedon, m.deletedby,
    m.alertenddatetime,
    m.memberprogramid,
    m.memberactivityid
FROM membernote m
WHERE m.membernoteid = @id;";

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

        public async Task<(List<MemberHealthNote> Items, int Total)>
            GetMemberHealthNotesForMemberAsync(long memberId, int page = 1, int pageSize = 25, bool includeDeleted = false)
        {
            // NOTE: 'memberId' is treated as MemberDetailsId for backward compatibility.
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 25;

            var filter = includeDeleted ? "" : "AND m.deletedon IS NULL";
            var sql = $@"
SELECT
    m.membernoteid,
    m.memberdetailsid,
    m.notetypeid,
    m.membernotes,
    m.enteredtimestamp,
    (m.isalert = B'1') as isalert_bool,
    (m.isexternal = B'1') as isexternal_bool,
    (m.displayinmemberportal = B'1') as displayportal_bool,
    m.activeflag,
    m.createdon, m.createdby, m.updatedon, m.updatedby, m.deletedon, m.deletedby,
    m.alertenddatetime,
    m.memberprogramid,
    m.memberactivityid,
    COUNT(*) OVER() AS total_count
FROM membernote m
WHERE m.memberdetailsid = @memberdetailsid
  {filter}
ORDER BY COALESCE(m.updatedon, m.createdon) DESC, m.membernoteid DESC
OFFSET @offset LIMIT @limit;";

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var items = new List<MemberHealthNote>();
            var total = 0;

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@memberdetailsid", memberId);
            cmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
            cmd.Parameters.AddWithValue("@limit", pageSize);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (total == 0 && !reader.IsDBNull(reader.GetOrdinal("total_count")))
                    total = reader.GetInt32(reader.GetOrdinal("total_count"));

                items.Add(Map(reader));
            }

            return (items, total);
        }

        public async Task<List<MemberHealthNote>> GetActiveAlertsForMemberAsync(long memberId)
        {
            const string sql = @"
SELECT
    m.membernoteid,
    m.memberdetailsid,
    m.notetypeid,
    m.membernotes,
    m.enteredtimestamp,
    (m.isalert = B'1') as isalert_bool,
    (m.isexternal = B'1') as isexternal_bool,
    (m.displayinmemberportal = B'1') as displayportal_bool,
    m.activeflag,
    m.createdon, m.createdby, m.updatedon, m.updatedby, m.deletedon, m.deletedby,
    m.alertenddatetime,
    m.memberprogramid,
    m.memberactivityid
FROM membernote m
WHERE m.memberdetailsid = @memberdetailsid
  AND m.isalert = B'1'
  AND m.deletedon IS NULL
ORDER BY COALESCE(m.updatedon, m.createdon) DESC;";

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@memberdetailsid", memberId);

            var list = new List<MemberHealthNote>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(Map(reader));
            }

            return list;
        }

        private static MemberHealthNote Map(NpgsqlDataReader reader)
        {
            return new MemberHealthNote
            {
                Id = reader.GetInt64(reader.GetOrdinal("membernoteid")),
                MemberDetailsId = reader.IsDBNull(reader.GetOrdinal("memberdetailsid")) ? (long?)null : reader.GetInt64(reader.GetOrdinal("memberdetailsid")),
                NoteTypeId = reader.IsDBNull(reader.GetOrdinal("notetypeid")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("notetypeid")),
                Notes = reader.IsDBNull(reader.GetOrdinal("membernotes")) ? "" : reader.GetString(reader.GetOrdinal("membernotes")),
                EnteredTimestamp = reader.IsDBNull(reader.GetOrdinal("enteredtimestamp")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("enteredtimestamp")),
                IsAlert = reader.GetBoolean(reader.GetOrdinal("isalert_bool")),
                IsExternal = reader.GetBoolean(reader.GetOrdinal("isexternal_bool")),
                DisplayInMemberPortal = reader.GetBoolean(reader.GetOrdinal("displayportal_bool")),
                ActiveFlag = reader.IsDBNull(reader.GetOrdinal("activeflag")) ? true : reader.GetBoolean(reader.GetOrdinal("activeflag")),
                CreatedOn = reader.GetDateTime(reader.GetOrdinal("createdon")),
                CreatedBy = reader.IsDBNull(reader.GetOrdinal("createdby")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("createdby")),
                UpdatedOn = reader.IsDBNull(reader.GetOrdinal("updatedon")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("updatedon")),
                UpdatedBy = reader.IsDBNull(reader.GetOrdinal("updatedby")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("updatedby")),
                DeletedOn = reader.IsDBNull(reader.GetOrdinal("deletedon")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("deletedon")),
                DeletedBy = reader.IsDBNull(reader.GetOrdinal("deletedby")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("deletedby")),
                AlertEndDateTime = reader.IsDBNull(reader.GetOrdinal("alertenddatetime")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("alertenddatetime")),
                MemberProgramId = reader.IsDBNull(reader.GetOrdinal("memberprogramid")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("memberprogramid")),
                MemberActivityId = reader.IsDBNull(reader.GetOrdinal("memberactivityid")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("memberactivityid"))
            };
        }
    }
}