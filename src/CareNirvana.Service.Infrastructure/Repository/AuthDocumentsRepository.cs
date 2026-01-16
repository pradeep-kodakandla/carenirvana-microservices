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
    public class AuthDocumentsRepository : IAuthDocumentsRepository
    {
        private const string DocsKey = "Auth_Documents_authDocumentsGrid";

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly string _connStr;

        public AuthDocumentsRepository(IConfiguration configuration)
        {
            _connStr = configuration.GetConnectionString("DefaultConnection");
        }

        private NpgsqlConnection CreateConn() => new NpgsqlConnection(_connStr);

        public async Task<IReadOnlyList<AuthDocumentDto>> GetDocumentsAsync(long authDetailId, CancellationToken ct = default)
        {
            var sql = $@"
                select coalesce(
                  (
                    select jsonb_agg(d order by (d->>'createdOn')::timestamptz desc)
                    from jsonb_array_elements(coalesce(a.data->'{DocsKey}','[]'::jsonb)) d
                    where d->>'deletedBy' is null
                  ),
                  '[]'::jsonb
                ) as documents
                from authdetail a
                where a.authdetailid = @authDetailId
                  and a.deletedon is null;";

            await using var conn = CreateConn();
            var docsJson = await conn.ExecuteScalarAsync<string>(
                new CommandDefinition(sql, new { authDetailId }, cancellationToken: ct));

            return JsonSerializer.Deserialize<List<AuthDocumentDto>>(docsJson ?? "[]", JsonOpts) ?? new List<AuthDocumentDto>();
        }

        public async Task<Guid> InsertDocumentAsync(long authDetailId, CreateAuthDocumentRequest req, int userId, CancellationToken ct = default)
        {
            var documentId = Guid.NewGuid();
            var now = DateTime.UtcNow;

            var newDoc = new AuthDocumentDto
            {
                DocumentId = documentId,
                DocumentType = req.DocumentType,
                DocumentLevel = req.DocumentLevel,
                DocumentDescription = req.DocumentDescription ?? "",
                FileNames = req.FileNames ?? new List<string>(),
                CreatedBy = userId,
                CreatedOn = now
            };

            var newDocJson = JsonSerializer.Serialize(newDoc, JsonOpts);

            var sql = $@"
                update authdetail a
                set data =
                  jsonb_set(
                    coalesce(a.data,'{{}}'::jsonb),
                    '{{{DocsKey}}}',
                    coalesce(a.data->'{DocsKey}','[]'::jsonb) || jsonb_build_array(@doc::jsonb),
                    true
                  ),
                  updatedon = now(),
                  updatedby = @userId
                where a.authdetailid = @authDetailId
                  and a.deletedon is null;";

            await using var conn = CreateConn();
            await LockAuthRow(conn, authDetailId, ct);

            var rows = await conn.ExecuteAsync(
                new CommandDefinition(sql, new { authDetailId, doc = newDocJson, userId }, cancellationToken: ct));

            if (rows == 0)
                throw new InvalidOperationException($"authdetail not found for authDetailId={authDetailId}");

            return documentId;
        }

        public async Task<bool> UpdateDocumentAsync(long authDetailId, Guid documentId, UpdateAuthDocumentRequest req, int userId, CancellationToken ct = default)
        {
            var patch = new Dictionary<string, object?>();

            if (req.DocumentType.HasValue) patch["documentType"] = req.DocumentType.Value;
            if (req.DocumentLevel.HasValue) patch["documentLevel"] = req.DocumentLevel.Value;
            if (req.DocumentDescription is not null) patch["documentDescription"] = req.DocumentDescription;
            if (req.FileNames is not null) patch["fileNames"] = req.FileNames;

            if (patch.Count == 0) return false;

            var patchJson = JsonSerializer.Serialize(patch, JsonOpts);

            var sql = $@"
                update authdetail a
                set data = jsonb_set(
                  coalesce(a.data,'{{}}'::jsonb),
                  '{{{DocsKey}}}',
                  (
                    select coalesce(
                      jsonb_agg(
                        case
                          when d->>'documentId' = @documentId::text then
                            (d || @patch::jsonb || jsonb_build_object('updatedOn', to_jsonb(now()), 'updatedBy', to_jsonb(@userId)))
                          else d
                        end
                      ),
                      '[]'::jsonb
                    )
                    from jsonb_array_elements(coalesce(a.data->'{DocsKey}','[]'::jsonb)) d
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
                new CommandDefinition(sql, new { authDetailId, documentId, patch = patchJson, userId }, cancellationToken: ct));

            return rows > 0;
        }

        public async Task<bool> SoftDeleteDocumentAsync(long authDetailId, Guid documentId, int userId, CancellationToken ct = default)
        {
            var sql = $@"
                update authdetail a
                set data = jsonb_set(
                  coalesce(a.data,'{{}}'::jsonb),
                  '{{{DocsKey}}}',
                  (
                    select coalesce(
                      jsonb_agg(
                        case
                          when d->>'documentId' = @documentId::text then
                            (d || jsonb_build_object('deletedBy', to_jsonb(@userId), 'deletedOn', to_jsonb(now())))
                          else d
                        end
                      ),
                      '[]'::jsonb
                    )
                    from jsonb_array_elements(coalesce(a.data->'{DocsKey}','[]'::jsonb)) d
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
                new CommandDefinition(sql, new { authDetailId, documentId, userId }, cancellationToken: ct));

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
