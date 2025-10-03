using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Data;

namespace CareNirvana.Service.Infrastructure.Repository
{

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
            INSERT INTO memberhealthnotes
            (memberid, notetypeid, notes, isalert, createdon, createdby, updatedon, updatedby)
            VALUES
            (@memberid, @notetypeid, @notes, @isalert, COALESCE(@createdon, NOW()), @createdby, @updatedon, @updatedby)
            RETURNING memberhealthnotesid;";

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@memberid", note.MemberId);
            cmd.Parameters.AddWithValue("@notetypeid", (object?)note.NoteTypeId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@notes", note.Notes ?? string.Empty);
            cmd.Parameters.AddWithValue("@isalert", note.IsAlert);
            cmd.Parameters.AddWithValue("@createdon", (object?)note.CreatedOn == null || note.CreatedOn == default ? DBNull.Value : note.CreatedOn);
            cmd.Parameters.AddWithValue("@createdby", (object?)note.CreatedBy ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@updatedon", (object?)note.UpdatedOn ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@updatedby", (object?)note.UpdatedBy ?? DBNull.Value);

            var id = await cmd.ExecuteScalarAsync();
            return (long)id!;
        }

        public async Task<int> UpdateMemberHealthNoteAsync(MemberHealthNote note)
        {
            const string sql = @"
            UPDATE memberhealthnotes
            SET
                notetypeid = @notetypeid,
                notes      = @notes,
                isalert    = @isalert,
                updatedon  = COALESCE(@updatedon, NOW()),
                updatedby  = @updatedby
            WHERE memberhealthnotesid = @id
              AND deletedon IS NULL;";

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", note.MemberHealthNotesId);
            cmd.Parameters.AddWithValue("@notetypeid", (object?)note.NoteTypeId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@notes", note.Notes ?? string.Empty);
            cmd.Parameters.AddWithValue("@isalert", note.IsAlert);
            cmd.Parameters.AddWithValue("@updatedon", (object?)note.UpdatedOn ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@updatedby", (object?)note.UpdatedBy ?? DBNull.Value);

            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task<int> SoftDeleteMemberHealthNoteAsync(long memberHealthNotesId, int deletedBy)
        {
            const string sql = @"
            UPDATE memberhealthnotes
            SET deletedon = NOW(),
                deletedby = @deletedby
            WHERE memberhealthnotesid = @id
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
            SELECT memberhealthnotesid, memberid, notetypeid, notes, isalert,
                   createdon, createdby, updatedby, updatedon, deletedby, deletedon
            FROM memberhealthnotes
            WHERE memberhealthnotesid = @id;";

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow);
            if (await reader.ReadAsync())
            {
                return new MemberHealthNote
                {
                    MemberHealthNotesId = reader.GetInt64(reader.GetOrdinal("memberhealthnotesid")),
                    MemberId = reader.GetInt64(reader.GetOrdinal("memberid")),
                    NoteTypeId = reader.IsDBNull(reader.GetOrdinal("notetypeid")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("notetypeid")),
                    Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? "" : reader.GetString(reader.GetOrdinal("notes")),
                    IsAlert = reader.GetBoolean(reader.GetOrdinal("isalert")),
                    CreatedOn = reader.GetDateTime(reader.GetOrdinal("createdon")),
                    CreatedBy = reader.IsDBNull(reader.GetOrdinal("createdby")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("createdby")),
                    UpdatedBy = reader.IsDBNull(reader.GetOrdinal("updatedby")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("updatedby")),
                    UpdatedOn = reader.IsDBNull(reader.GetOrdinal("updatedon")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("updatedon")),
                    DeletedBy = reader.IsDBNull(reader.GetOrdinal("deletedby")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("deletedby")),
                    DeletedOn = reader.IsDBNull(reader.GetOrdinal("deletedon")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("deletedon"))
                };
            }
            return null;
        }

        public async Task<(List<MemberHealthNote> Items, int Total)>
            GetMemberHealthNotesForMemberAsync(long memberId, int page = 1, int pageSize = 25, bool includeDeleted = false)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 25;

            // Simple paged list ordered by newest first
            var results = new List<MemberHealthNote>();
            var total = 0;

            var filter = includeDeleted ? "" : "AND mhn.deletedon IS NULL";
            var sql = $@"
            SELECT
                mhn.memberhealthnotesid, mhn.memberid, mhn.notetypeid, mhn.notes, mhn.isalert,
                mhn.createdon, mhn.createdby, mhn.updatedby, mhn.updatedon, mhn.deletedby, mhn.deletedon,
                COUNT(*) OVER() AS total_count
            FROM memberhealthnotes mhn
            WHERE mhn.memberid = @memberid
              {filter}
            ORDER BY COALESCE(mhn.updatedon, mhn.createdon) DESC, mhn.memberhealthnotesid DESC
            OFFSET @offset LIMIT @limit;";

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@memberid", memberId);
            cmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
            cmd.Parameters.AddWithValue("@limit", pageSize);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (total == 0 && !reader.IsDBNull(reader.GetOrdinal("total_count")))
                    total = reader.GetInt32(reader.GetOrdinal("total_count"));

                var note = new MemberHealthNote
                {
                    MemberHealthNotesId = reader.GetInt64(reader.GetOrdinal("memberhealthnotesid")),
                    MemberId = reader.GetInt64(reader.GetOrdinal("memberid")),
                    NoteTypeId = reader.IsDBNull(reader.GetOrdinal("notetypeid")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("notetypeid")),
                    Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? "" : reader.GetString(reader.GetOrdinal("notes")),
                    IsAlert = reader.GetBoolean(reader.GetOrdinal("isalert")),
                    CreatedOn = reader.GetDateTime(reader.GetOrdinal("createdon")),
                    CreatedBy = reader.IsDBNull(reader.GetOrdinal("createdby")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("createdby")),
                    UpdatedBy = reader.IsDBNull(reader.GetOrdinal("updatedby")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("updatedby")),
                    UpdatedOn = reader.IsDBNull(reader.GetOrdinal("updatedon")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("updatedon")),
                    DeletedBy = reader.IsDBNull(reader.GetOrdinal("deletedby")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("deletedby")),
                    DeletedOn = reader.IsDBNull(reader.GetOrdinal("deletedon")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("deletedon"))
                };
                results.Add(note);
            }

            return (results, total);
        }

        public async Task<List<MemberHealthNote>> GetActiveAlertsForMemberAsync(long memberId)
        {
            const string sql = @"
            SELECT memberhealthnotesid, memberid, notetypeid, notes, isalert,
                   createdon, createdby, updatedby, updatedon, deletedby, deletedon
            FROM memberhealthnotes
            WHERE memberid = @memberid
              AND isalert = TRUE
              AND deletedon IS NULL
            ORDER BY COALESCE(updatedon, createdon) DESC;";

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@memberid", memberId);

            var list = new List<MemberHealthNote>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new MemberHealthNote
                {
                    MemberHealthNotesId = reader.GetInt64(reader.GetOrdinal("memberhealthnotesid")),
                    MemberId = reader.GetInt64(reader.GetOrdinal("memberid")),
                    NoteTypeId = reader.IsDBNull(reader.GetOrdinal("notetypeid")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("notetypeid")),
                    Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? "" : reader.GetString(reader.GetOrdinal("notes")),
                    IsAlert = reader.GetBoolean(reader.GetOrdinal("isalert")),
                    CreatedOn = reader.GetDateTime(reader.GetOrdinal("createdon")),
                    CreatedBy = reader.IsDBNull(reader.GetOrdinal("createdby")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("createdby")),
                    UpdatedBy = reader.IsDBNull(reader.GetOrdinal("updatedby")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("updatedby")),
                    UpdatedOn = reader.IsDBNull(reader.GetOrdinal("updatedon")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("updatedon")),
                    DeletedBy = reader.IsDBNull(reader.GetOrdinal("deletedby")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("deletedby")),
                    DeletedOn = reader.IsDBNull(reader.GetOrdinal("deletedon")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("deletedon"))
                });
            }

            return list;
        }
    }
}
