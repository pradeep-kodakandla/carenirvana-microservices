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
        private const string NotesKey = "authNotes";

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

        public async Task<TemplateSectionResponse?> GetAuthNotesTemplateAsync(int authTemplateId, CancellationToken ct = default)
        {
            const string sql = @"
                SELECT jsonb_path_query_first(
                         ct.jsoncontent::jsonb,
                         '$.** ? (@.sectionName == $name)',
                         jsonb_build_object('name', to_jsonb(@sectionName))
                       )::text AS section
                FROM cfgauthtemplate ct
                WHERE ct.authtemplateid = @authTemplateId;";

            await using var conn = CreateConn();

            var sectionJson = await conn.ExecuteScalarAsync<string>(
                new CommandDefinition(sql,
                    new { authTemplateId, sectionName = "Authorization Notes" },
                    cancellationToken: ct)
            );

            if (string.IsNullOrWhiteSpace(sectionJson))
                return null;

            using var doc = JsonDocument.Parse(sectionJson);

            return new TemplateSectionResponse
            {
                CaseTemplateId = authTemplateId, // rename property if you want (see note below)
                SectionName = "Authorization Notes",
                Section = doc.RootElement.Clone()
            };
        }

        public async Task<IReadOnlyList<AuthNoteDto>> GetNotesAsync(long authDetailId, CancellationToken ct = default)
        {
            const string notesKey = "authNotes";

            const string sql = @"
                select coalesce(
                  (
                    select jsonb_agg(n order by (n->>'createdOn')::timestamptz desc)
                    from jsonb_array_elements(coalesce(a.data->@notesKey, '[]'::jsonb)) n
                    where n->>'deletedBy' is null
                  ),
                  '[]'::jsonb
                )::text as notes
                from authdetail a
                where a.authdetailid = @authDetailId
                  and a.deletedon is null;";

            await using var conn = CreateConn();
            var notesJson = await conn.ExecuteScalarAsync<string>(
                new CommandDefinition(sql, new { authDetailId, notesKey }, cancellationToken: ct));

            return JsonSerializer.Deserialize<List<AuthNoteDto>>(notesJson ?? "[]", JsonOpts) ?? new List<AuthNoteDto>();
        }


        public async Task<Guid> InsertNoteAsync(long authDetailId, CreateAuthNoteRequest req, int userId, CancellationToken ct = default)
        {
            var noteId = Guid.NewGuid();

            const string sql = @"
                  update authdetail a
                        set data = jsonb_set(
                            coalesce(a.data, '{}'::jsonb),
                            '{authNotes}',
                            coalesce(a.data->'authNotes', '[]'::jsonb) || jsonb_build_object(
                              'noteId', @noteId::text,
                              'noteText', coalesce(@noteText, ''),
                              'noteType', to_jsonb(@noteType::int),
                              'authAlertNote', to_jsonb(@authAlertNote::boolean),
                              'encounteredOn', to_jsonb(@encounteredOn::timestamp),
                              'alertEndDate', to_jsonb(@alertEndDate::timestamp),
                              'createdBy', @userId,
                              'createdOn', now(),
                              'updatedBy', null,
                              'updatedOn', null,
                              'deletedBy', null,
                              'deletedOn', null
                            ),
                            true
                        ),
                        updatedon = now(),
                        updatedby = @userId
                        where a.authdetailid = @authDetailId
                          and a.deletedon is null;";

            await using var conn = CreateConn();
            var rows = await conn.ExecuteAsync(new CommandDefinition(sql, new
            {
                authDetailId,
                noteId,
                noteText = req.NoteText,
                noteType = req.NoteType,
                authAlertNote = req.EncounteredOn.HasValue ? true : false,
                encounteredOn = req.EncounteredOn,
                alertEndDate = req.AlertEndDate,
                userId
            }, cancellationToken: ct));

            if (rows == 0) throw new InvalidOperationException("Auth not found or deleted.");

            return noteId;
        }

        public async Task<bool> UpdateNoteAsync(long authDetailId, Guid noteId, UpdateAuthNoteRequest req, int userId, CancellationToken ct = default)
        {
            const string sql = @"
                  update authdetail a
                  set data = jsonb_set(
                      coalesce(a.data, '{}'::jsonb),
                      '{authNotes}',
                      coalesce((
                        select jsonb_agg(
                          case
                            when n->>'noteId' = @noteId::text then
                              (n
                                || jsonb_build_object(
                                  'noteText', coalesce(@noteText, n->>'noteText'),
                                  'noteType', coalesce(to_jsonb(@noteType), n->'noteType'),
                                  'authAlertNote', coalesce(to_jsonb(@authAlertNote), n->'authAlertNote'),
                                  'encounteredOn', coalesce(to_jsonb(@encounteredOn), n->'encounteredOn'),
                                  'alertEndDate', coalesce(to_jsonb(@alertEndDate), n->'alertEndDate'),
                                  'updatedBy', @userId,
                                  'updatedOn', now()
                                )
                              )
                            else n
                          end
                          order by (n->>'createdOn')::timestamptz desc
                        )
                        from jsonb_array_elements(coalesce(a.data->'authNotes', '[]'::jsonb)) n
                      ), '[]'::jsonb),
                      true
                  ),
                  updatedon = now(),
                  updatedby = @userId
                  where a.authdetailid = @authDetailId
                    and a.deletedon is null;";

            await using var conn = CreateConn();
            var rows = await conn.ExecuteAsync(new CommandDefinition(sql, new
            {
                authDetailId,
                noteId,
                noteText = req.NoteText,
                noteType = req.NoteType,
                authAlertNote = req.EncounteredOn.HasValue ? true : false,
                encounteredOn = req.EncounteredOn,
                alertEndDate = req.AlertEndDate,
                userId
            }, cancellationToken: ct));

            if (rows == 0) throw new InvalidOperationException("Auth not found or deleted.");
            else return rows > 0;
        }

        public async Task<bool> SoftDeleteNoteAsync(long authDetailId, Guid noteId, int userId, CancellationToken ct = default)
        {
            const string sql = @"
              update authdetail a
              set data = jsonb_set(
                  coalesce(a.data, '{}'::jsonb),
                  '{authNotes}',
                  coalesce((
                    select jsonb_agg(
                      case
                        when n->>'noteId' = @noteId::text then
                          (n
                            || jsonb_build_object(
                              'deletedBy', @userId,
                              'deletedOn', now()
                            )
                          )
                        else n
                      end
                      order by (n->>'createdOn')::timestamptz desc
                    )
                    from jsonb_array_elements(coalesce(a.data->'authNotes', '[]'::jsonb)) n
                  ), '[]'::jsonb),
                  true
              ),
              updatedon = now(),
              updatedby = @userId
              where a.authdetailid = @authDetailId
                and a.deletedon is null;";

            await using var conn = CreateConn();
            var rows = await conn.ExecuteAsync(new CommandDefinition(sql, new
            {
                authDetailId,
                noteId,
                userId
            }, cancellationToken: ct));

            if (rows == 0) throw new InvalidOperationException("Auth not found or deleted.");
            else
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
