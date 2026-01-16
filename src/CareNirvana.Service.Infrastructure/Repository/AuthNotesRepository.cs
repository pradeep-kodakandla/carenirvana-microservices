using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CareNirvana.Service.Infrastructure.Repository
{
    public class AuthNotesRepository : IAuthNotesRepository
    {
        private const string NotesKey = "Auth_Notes_authNotesGrid";

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly string _connStr;

        public AuthNotesRepository(IConfiguration configuration)
        {
            _connStr = configuration.GetConnectionString("DefaultConnection");
        }

        private NpgsqlConnection CreateConn() => new NpgsqlConnection(_connStr);

        public async Task<IReadOnlyList<AuthNoteDto>> GetNotesAsync(long authDetailId, CancellationToken ct = default)
        {
            var sql = $@"
                select coalesce(
                  (
                    select jsonb_agg(n order by (n->>'createdOn')::timestamptz desc)
                    from jsonb_array_elements(coalesce(a.data->'{NotesKey}','[]'::jsonb)) n
                    where n->>'deletedBy' is null
                  ),
                  '[]'::jsonb
                ) as notes
                from authdetail a
                where a.authdetailid = @authDetailId
                  and a.deletedon is null;";

            await using var conn = CreateConn();
            var notesJson = await conn.ExecuteScalarAsync<string>(
                new CommandDefinition(sql, new { authDetailId }, cancellationToken: ct));

            return JsonSerializer.Deserialize<List<AuthNoteDto>>(notesJson ?? "[]", JsonOpts) ?? new List<AuthNoteDto>();
        }

        public async Task<Guid> InsertNoteAsync(long authDetailId, CreateAuthNoteRequest req, int userId, CancellationToken ct = default)
        {
            var noteId = Guid.NewGuid();
            var now = DateTime.UtcNow;

            var newNote = new AuthNoteDto
            {
                NoteId = noteId,
                NoteText = req.NoteText ?? "",
                NoteLevel = req.NoteLevel,
                NoteType = req.NoteType,
                AuthAlertNote = req.AuthAlertNote,
                CreatedBy = userId,
                CreatedOn = now
            };

            var newNoteJson = JsonSerializer.Serialize(newNote, JsonOpts);

            var sql = $@"
                update authdetail a
                set data =
                  jsonb_set(
                    coalesce(a.data,'{{}}'::jsonb),
                    '{{{NotesKey}}}',
                    coalesce(a.data->'{NotesKey}','[]'::jsonb) || jsonb_build_array(@newNote::jsonb),
                    true
                  ),
                  updatedon = now(),
                  updatedby = @userId
                where a.authdetailid = @authDetailId
                  and a.deletedon is null;";

            await using var conn = CreateConn();
            await LockAuthRow(conn, authDetailId, ct);

            var rows = await conn.ExecuteAsync(
                new CommandDefinition(sql, new { authDetailId, newNote = newNoteJson, userId }, cancellationToken: ct));

            if (rows == 0)
                throw new InvalidOperationException($"authdetail not found for authDetailId={authDetailId}");

            return noteId;
        }

        public async Task<bool> UpdateNoteAsync(long authDetailId, Guid noteId, UpdateAuthNoteRequest req, int userId, CancellationToken ct = default)
        {
            var patch = new Dictionary<string, object?>();

            if (req.NoteText is not null) patch["noteText"] = req.NoteText;
            if (req.NoteLevel.HasValue) patch["noteLevel"] = req.NoteLevel.Value;
            if (req.NoteType.HasValue) patch["noteType"] = req.NoteType.Value;
            if (req.AuthAlertNote.HasValue) patch["authAlertNote"] = req.AuthAlertNote.Value;

            if (patch.Count == 0) return false;

            var patchJson = JsonSerializer.Serialize(patch, JsonOpts);

            var sql = $@"
                update authdetail a
                set data = jsonb_set(
                  coalesce(a.data,'{{}}'::jsonb),
                  '{{{NotesKey}}}',
                  (
                    select coalesce(
                      jsonb_agg(
                        case
                          when n->>'noteId' = @noteId::text then
                            (n || @patch::jsonb || jsonb_build_object('updatedOn', to_jsonb(now()), 'updatedBy', to_jsonb(@userId)))
                          else n
                        end
                      ),
                      '[]'::jsonb
                    )
                    from jsonb_array_elements(coalesce(a.data->'{NotesKey}','[]'::jsonb)) n
                  ),
                  true
                ),
                updatedon = now(),
                updatedby = @userId
                where a.authdetailid = @authDetailId
                  and a.deletedon is null;";

            await using var conn = CreateConn();
            await LockAuthRow(conn, authDetailId, ct);

            var rows = await conn.ExecuteAsync(
                new CommandDefinition(sql, new { authDetailId, noteId, patch = patchJson, userId }, cancellationToken: ct));

            return rows > 0;
        }

        public async Task<bool> SoftDeleteNoteAsync(long authDetailId, Guid noteId, int userId, CancellationToken ct = default)
        {
            var sql = $@"
                update authdetail a
                set data = jsonb_set(
                  coalesce(a.data,'{{}}'::jsonb),
                  '{{{NotesKey}}}',
                  (
                    select coalesce(
                      jsonb_agg(
                        case
                          when n->>'noteId' = @noteId::text then
                            (n || jsonb_build_object('deletedBy', to_jsonb(@userId), 'deletedOn', to_jsonb(now())))
                          else n
                        end
                      ),
                      '[]'::jsonb
                    )
                    from jsonb_array_elements(coalesce(a.data->'{NotesKey}','[]'::jsonb)) n
                  ),
                  true
                ),
                updatedon = now(),
                updatedby = @userId
                where a.authdetailid = @authDetailId
                  and a.deletedon is null;";

            await using var conn = CreateConn();
            await LockAuthRow(conn, authDetailId, ct);

            var rows = await conn.ExecuteAsync(
                new CommandDefinition(sql, new { authDetailId, noteId, userId }, cancellationToken: ct));

            return rows > 0;
        }

        private static async Task LockAuthRow(NpgsqlConnection conn, long authDetailId, CancellationToken ct)
        {
            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync(ct);

            const string lockSql = @"
                select 1
                from authdetail
                where authdetailid = @authDetailId
                  and deletedon is null
                for update;";

            await conn.ExecuteAsync(new CommandDefinition(lockSql, new { authDetailId }, cancellationToken: ct));
        }
    }
}
