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
        private const string DocsKey = "authDocuments";

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

        public async Task<TemplateSectionResponse?> GetAuthDocumentsTemplateAsync(int authTemplateId, CancellationToken ct = default)
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
                    new { authTemplateId, sectionName = "Authorization Documents" },
                    cancellationToken: ct)
            );

            if (string.IsNullOrWhiteSpace(sectionJson))
                return null;

            using var doc = JsonDocument.Parse(sectionJson);

            return new TemplateSectionResponse
            {
                CaseTemplateId = authTemplateId,
                SectionName = "Authorization Documents",
                Section = doc.RootElement.Clone()
            };
        }

        public async Task<IReadOnlyList<AuthDocumentDto>> GetDocumentsAsync(long authDetailId, CancellationToken ct = default)
        {
            const string sql = @"
                  select coalesce(
                    (
                      select jsonb_agg(d order by (d->>'createdOn')::timestamptz desc)
                      from jsonb_array_elements(coalesce(a.data->'authDocuments','[]'::jsonb)) d
                      where d->>'deletedBy' is null
                    ),
                    '[]'::jsonb
                  )::text as documents
                  from authdetail a
                  where a.authdetailid = @authDetailId
                    and a.deletedon is null;";

            await using var conn = CreateConn();
            var json = await conn.ExecuteScalarAsync<string>(
                new CommandDefinition(sql, new { authDetailId }, cancellationToken: ct));

            return JsonSerializer.Deserialize<List<AuthDocumentDto>>(json ?? "[]", JsonOpts) ?? new();
        }


        public async Task<Guid> InsertDocumentAsync(long authDetailId, CreateAuthDocumentRequest req, int userId, CancellationToken ct = default)
        {
            var documentId = Guid.NewGuid();

            const string sql = @"
                  update authdetail a
                  set data = jsonb_set(
                      coalesce(a.data, '{}'::jsonb),
                      '{authDocuments}',
                      coalesce(a.data->'authDocuments', '[]'::jsonb) || jsonb_build_object(
                        'documentId', @documentId::text,
                        'documentType', @documentType,
                        'documentDescription', coalesce(@documentDescription, ''),
                        'fileNames', coalesce(to_jsonb(@fileNames::text[]), '[]'::jsonb),
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
                documentId,
                documentType = req.DocumentType,
                documentDescription = req.DocumentDescription,
                fileNames = (req.FileNames ?? new List<string>()).ToArray(),
                userId
            }, cancellationToken: ct));

            if (rows == 0) throw new InvalidOperationException("Auth not found or deleted.");

            return documentId;
        }

        public async Task<bool> UpdateDocumentAsync(long authDetailId, Guid documentId, UpdateAuthDocumentRequest req, int userId, CancellationToken ct = default)
        {
            const string sql = @"
                  update authdetail a
                  set data = jsonb_set(
                    coalesce(a.data, '{}'::jsonb),
                    '{authDocuments}',
                    coalesce((
                      select jsonb_agg(
                        case
                          when d->>'documentId' = @documentId::text then
                            (d
                              || jsonb_build_object(
                                'documentType', coalesce(to_jsonb(@documentType), d->'documentType'),
                                'documentDescription', coalesce(@documentDescription, d->>'documentDescription'),
                                'fileNames', coalesce(to_jsonb(@fileNames::text[]), d->'fileNames'),
                                'updatedBy', @userId,
                                'updatedOn', now()
                              )
                            )
                          else d
                        end
                        order by (d->>'createdOn')::timestamptz desc
                      )
                      from jsonb_array_elements(coalesce(a.data->'authDocuments','[]'::jsonb)) d
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
                documentId,
                documentType = req.DocumentType,
                documentDescription = req.DocumentDescription,
                fileNames = (req.FileNames ?? new List<string>()).ToArray(),
                userId
            }, cancellationToken: ct));

            if (rows == 0) throw new InvalidOperationException("Auth not found or deleted.");
            else
                return rows > 0;
        }

        public async Task<bool> SoftDeleteDocumentAsync(long authDetailId, Guid documentId, int userId, CancellationToken ct = default)
        {
            const string sql = @"
                  update authdetail a
                  set data = jsonb_set(
                    coalesce(a.data, '{}'::jsonb),
                    '{authDocuments}',
                    coalesce((
                      select jsonb_agg(
                        case
                          when d->>'documentId' = @documentId::text then
                            (d || jsonb_build_object('deletedBy', @userId, 'deletedOn', now()))
                          else d
                        end
                        order by (d->>'createdOn')::timestamptz desc
                      )
                      from jsonb_array_elements(coalesce(a.data->'authDocuments','[]'::jsonb)) d
                    ), '[]'::jsonb),
                    true
                  ),
                  updatedon = now(),
                  updatedby = @userId
                  where a.authdetailid = @authDetailId
                    and a.deletedon is null;";

            await using var conn = CreateConn();
            var rows = await conn.ExecuteAsync(new CommandDefinition(sql, new { authDetailId, documentId, userId }, cancellationToken: ct));
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
