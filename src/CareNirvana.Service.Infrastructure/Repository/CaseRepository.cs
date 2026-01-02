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
    public class CaseRepository : ICaseRepository
    {
        private readonly string _connStr;

        public CaseRepository(IConfiguration configuration)
        {
            _connStr = configuration.GetConnectionString("DefaultConnection");
        }

        private NpgsqlConnection CreateConn() => new NpgsqlConnection(_connStr);
        public async Task<CaseAggregate?> GetCaseByNumberAsync(string caseNumber, bool includeDeleted = false)
        {
            const string headerSql = @"
                select h.caseheaderid  as CaseHeaderId,
                       h.casenumber    as CaseNumber,
                       h.casetype      as CaseType,
                       h.status        as Status,
                       h.memberdetailid as MemberDetailId,
                       h.createdon     as CreatedOn,
                       h.createdby     as CreatedBy,
                       h.updatedon     as UpdatedOn,
                       h.updatedby     as UpdatedBy,
                       h.deletedon     as DeletedOn,
                       h.deletedby     as DeletedBy,
                       su.username     as CreatedByUserName,
                       md.memberid     as MemberId,
                       (coalesce(md.firstname,'') || case when md.lastname is null or md.lastname = '' then '' else ' ' || md.lastname end) as MemberName
                from caseheader h
                left join securityuser su on su.userid = h.createdby
                left join memberdetails md on md.memberdetailsid = h.memberdetailid
                where h.casenumber = @caseNumber
                  and (@includeDeleted = true or h.deletedon is null);";

            const string detailsSql = @"
                select d.casedetailid    as CaseDetailId,
                       d.caseheaderid    as CaseHeaderId,
                       d.caselevelid     as CaseLevelId,
                       d.caselevelnumber as CaseLevelNumber,
                       d.jsondata::text  as JsonData,
                       d.createdon       as CreatedOn,
                       d.createdby       as CreatedBy,
                       d.updatedon       as UpdatedOn,
                       d.updatedby       as UpdatedBy,
                       d.deletedon       as DeletedOn,
                       d.deletedby       as DeletedBy
                from casedetail d
                join caseheader h on h.caseheaderid = d.caseheaderid
                where h.casenumber = @caseNumber
                  and (@includeDeleted = true or d.deletedon is null)
                order by d.caselevelid;";

            await using var conn = CreateConn();

            var header = await conn.QueryFirstOrDefaultAsync<CaseHeader>(
                headerSql, new { caseNumber, includeDeleted });

            if (header == null) return null;

            var details = (await conn.QueryAsync<CaseDetail>(
                detailsSql, new { caseNumber, includeDeleted })).AsList();

            return new CaseAggregate { Header = header, Details = details };
        }


        public async Task<CaseAggregate?> GetCaseByHeaderIdAsync(long caseHeaderId, bool includeDeleted = false)
        {
            const string headerSql = @"
                select h.caseheaderid   as CaseHeaderId,
                       h.casenumber     as CaseNumber,
                       h.casetype       as CaseType,
                       h.status         as Status,
                       h.memberdetailid as MemberDetailId,
                       h.createdon      as CreatedOn,
                       h.createdby      as CreatedBy,
                       h.updatedon      as UpdatedOn,
                       h.updatedby      as UpdatedBy,
                       h.deletedon      as DeletedOn,
                       h.deletedby      as DeletedBy,
                       su.username      as CreatedByUserName,
                       md.memberid      as MemberId,
                       (coalesce(md.firstname,'') || case when md.lastname is null or md.lastname = '' then '' else ' ' || md.lastname end) as MemberName
                from caseheader h
                left join securityuser su on su.userid = h.createdby
                left join memberdetails md on md.memberdetailsid = h.memberdetailid
                where h.caseheaderid = @caseHeaderId
                  and (@includeDeleted = true or h.deletedon is null);";

            const string detailsSql = @"
                select d.casedetailid    as CaseDetailId,
                       d.caseheaderid    as CaseHeaderId,
                       d.caselevelid     as CaseLevelId,
                       d.caselevelnumber as CaseLevelNumber,
                       d.jsondata::text  as JsonData,
                       d.createdon       as CreatedOn,
                       d.createdby       as CreatedBy,
                       d.updatedon       as UpdatedOn,
                       d.updatedby       as UpdatedBy,
                       d.deletedon       as DeletedOn,
                       d.deletedby       as DeletedBy
                from casedetail d
                where d.caseheaderid = @caseHeaderId
                  and (@includeDeleted = true or d.deletedon is null)
                order by d.caselevelid;";

            await using var conn = CreateConn();

            var header = await conn.QueryFirstOrDefaultAsync<CaseHeader>(
                headerSql, new { caseHeaderId, includeDeleted });

            if (header == null) return null;

            var details = (await conn.QueryAsync<CaseDetail>(
                detailsSql, new { caseHeaderId, includeDeleted })).AsList();

            return new CaseAggregate { Header = header, Details = details };
        }

        public async Task<List<CaseAggregate>> GetCasesByMemberDetailIdAsync(
            long memberDetailId,
            bool includeDetails = false,
            IEnumerable<string>? statuses = null,
            bool includeDeleted = false)
        {
            // IMPORTANT: Postgres array filtering uses = any(@statuses)
            // Dapper will send string[] properly.
            var statusArr = statuses?.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToArray();

            const string headersSql = @"
                select h.caseheaderid   as CaseHeaderId,
                       h.casenumber     as CaseNumber,
                       h.casetype       as CaseType,
                       h.status         as Status,
                       h.memberdetailid as MemberDetailId,
                       h.createdon      as CreatedOn,
                       h.createdby      as CreatedBy,
                       h.updatedon      as UpdatedOn,
                       h.updatedby      as UpdatedBy,
                       h.deletedon      as DeletedOn,
                       h.deletedby      as DeletedBy,

                       su.username      as CreatedByUserName,
                       md.memberid      as MemberId,
                       (coalesce(md.firstname,'') || case when md.lastname is null or md.lastname = '' then '' else ' ' || md.lastname end) as MemberName
                from caseheader h
                left join securityuser su on su.userid = h.createdby
                left join memberdetails md on md.memberdetailsid = h.memberdetailid
                where h.memberdetailid = @memberDetailId
                  and (@includeDeleted = true or h.deletedon is null)
                  and (
                        @statusArr is null
                        or cardinality(@statusArr) = 0
                        or h.status = any(@statusArr)
                      )
                order by h.createdon desc;";

            const string detailsByHeaderSql = @"
                select d.casedetailid    as CaseDetailId,
                       d.caseheaderid    as CaseHeaderId,
                       d.caselevelid     as CaseLevelId,
                       d.caselevelnumber as CaseLevelNumber,
                       d.jsondata::text  as JsonData,
                       d.createdon       as CreatedOn,
                       d.createdby       as CreatedBy,
                       d.updatedon       as UpdatedOn,
                       d.updatedby       as UpdatedBy,
                       d.deletedon       as DeletedOn,
                       d.deletedby       as DeletedBy
                from casedetail d
                where d.caseheaderid = @caseHeaderId
                  and (@includeDeleted = true or d.deletedon is null)
                order by d.caselevelid;";

            await using var conn = CreateConn();

            var headers = (await conn.QueryAsync<CaseHeader>(
                headersSql, new { memberDetailId, includeDetails, includeDeleted, statusArr })).AsList();

            if (!includeDetails)
                return headers.Select(h => new CaseAggregate { Header = h, Details = new List<CaseDetail>() }).ToList();

            // load details per header (simple + clear). If you want, I can optimize into a single query.
            var result = new List<CaseAggregate>(headers.Count);

            foreach (var h in headers)
            {
                var details = (await conn.QueryAsync<CaseDetail>(
                    detailsByHeaderSql, new { caseHeaderId = h.CaseHeaderId, includeDeleted })).AsList();

                result.Add(new CaseAggregate { Header = h, Details = details });
            }

            return result;
        }

        /// First insert: insert caseheader + insert casedetail using LevelId (assumed to be 1 for Level1)
        public async Task<CreateCaseResult> CreateCaseAsync(CreateCaseRequest req, long userId)
        {
            const string findHeaderSql = @"
                select caseheaderid as CaseHeaderId,
                       casenumber   as CaseNumber,
                       memberdetailid as MemberDetailId
                from caseheader
                where casenumber = @caseNumber and deletedon is null;";

            const string insertHeaderSql = @"
                insert into caseheader (casenumber, casetype, status,memberdetailid, createdon, createdby)
                values (@caseNumber, @caseType, @status, @memberDetailId, now(), @userId)
                returning caseheaderid as CaseHeaderId, casenumber as CaseNumber;";

            const string insertDetailSql = @"
                insert into casedetail (caseheaderid, caselevelid, caselevelnumber, jsondata, createdon, createdby)
                values (@caseHeaderId, @caseLevelId, @caseLevelNumber, @jsonData::jsonb, now(), @userId)
                returning casedetailid;";

            await using var conn = CreateConn();
            await conn.OpenAsync();

            await using var tx = await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted);

            var existing = await conn.QueryFirstOrDefaultAsync<CaseHeader>(
                findHeaderSql,
                new { caseNumber = req.CaseNumber },
                tx);

            long caseHeaderId;
            string caseNumber;

            if (existing != null)
            {
                caseHeaderId = existing.CaseHeaderId;
                caseNumber = existing.CaseNumber;
            }
            else
            {
                var inserted = await conn.QuerySingleAsync<CaseHeader>(
                    insertHeaderSql,
                    new
                    {
                        caseNumber = req.CaseNumber,
                        caseType = req.CaseType,
                        status = req.Status,
                        memberDetailId = req.MemberDetailId,
                        userId
                    },
                    tx);

                caseHeaderId = inserted.CaseHeaderId;
                caseNumber = inserted.CaseNumber;
            }

            // Example: 51471L1, 51471L2
            var caseLevelNumber = $"{caseNumber}L{req.LevelId}";

            var caseDetailId = await conn.ExecuteScalarAsync<long>(
                insertDetailSql,
                new
                {
                    caseHeaderId,
                    caseLevelId = req.LevelId,
                    caseLevelNumber,
                    jsonData = req.JsonData,
                    userId
                },
                tx);

            await tx.CommitAsync();

            return new CreateCaseResult
            {
                CaseHeaderId = caseHeaderId,
                CaseNumber = caseNumber,
                CaseDetailId = caseDetailId,
                CaseLevelNumber = caseLevelNumber
            };
        }

        public async Task<AddLevelResult> AddCaseLevelAsync(AddCaseLevelRequest req, long userId)
        {
            const string headerSql = @"
                select casenumber
                from caseheader
                where caseheaderid = @caseHeaderId and deletedon is null;";

            const string insertDetailSql = @"
                insert into casedetail (caseheaderid, caselevelid, caselevelnumber, jsondata, createdon, createdby)
                values (@caseHeaderId, @caseLevelId, @caseLevelNumber, @jsonData::jsonb, now(), @userId)
                returning casedetailid;";

            await using var conn = CreateConn();
            await conn.OpenAsync();

            var caseNumber = await conn.ExecuteScalarAsync<string?>(
                headerSql,
                new { caseHeaderId = req.CaseHeaderId });

            if (string.IsNullOrWhiteSpace(caseNumber))
                throw new InvalidOperationException($"Case header not found for CaseHeaderId={req.CaseHeaderId}");

            var caseLevelNumber = $"{caseNumber}L{req.LevelId}";

            var caseDetailId = await conn.ExecuteScalarAsync<long>(
                insertDetailSql,
                new
                {
                    caseHeaderId = req.CaseHeaderId,
                    caseLevelId = req.LevelId,
                    caseLevelNumber,
                    jsonData = req.JsonData,
                    userId
                });

            return new AddLevelResult
            {
                CaseHeaderId = req.CaseHeaderId,
                CaseNumber = caseNumber,
                CaseDetailId = caseDetailId,
                CaseLevelNumber = caseLevelNumber
            };
        }

        /// Update only casedetail
        public async Task UpdateCaseDetailAsync(UpdateCaseDetailRequest req, long userId)
        {
            const string sql = @"
                update casedetail
                set jsondata    = @jsonData::jsonb,
                    caselevelid = coalesce(@caseLevelId, caselevelid),
                    updatedon   = now(),
                    updatedby   = @userId
                where casedetailid = @caseDetailId
                  and deletedon is null;";

            await using var conn = CreateConn();
            await conn.ExecuteAsync(sql, new
            {
                caseDetailId = req.CaseDetailId,
                jsonData = req.JsonData,
                caseLevelId = req.LevelId,
                userId
            });
        }

        public async Task SoftDeleteCaseHeaderAsync(long caseHeaderId, long userId, bool cascadeDetails = true)
        {
            const string headerSql = @"
                update caseheader
                set deletedon = now(), deletedby = @userId
                where caseheaderid = @caseHeaderId
                  and deletedon is null;";

            const string detailsSql = @"
                update casedetail
                set deletedon = now(), deletedby = @userId
                where caseheaderid = @caseHeaderId
                  and deletedon is null;";

            await using var conn = CreateConn();
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            await conn.ExecuteAsync(headerSql, new { caseHeaderId, userId }, tx);

            if (cascadeDetails)
                await conn.ExecuteAsync(detailsSql, new { caseHeaderId, userId }, tx);

            await tx.CommitAsync();
        }

        public async Task SoftDeleteCaseDetailAsync(long caseDetailId, long userId)
        {
            const string sql = @"
                update casedetail
                set deletedon = now(), deletedby = @userId
                where casedetailid = @caseDetailId
                  and deletedon is null;";

            await using var conn = CreateConn();
            await conn.ExecuteAsync(sql, new { caseDetailId, userId });
        }

        public async Task<IReadOnlyList<AgCaseRow>> GetAgCasesByMemberAsync(int memberDetailId, CancellationToken ct = default)
        {
            const string sql = @"
                                WITH admin AS (
                                  SELECT jsoncontent::jsonb AS j
                                  FROM cfgadmindata
                                  WHERE module = 'AG'
                                  ORDER BY COALESCE(updatedon, createdon) DESC NULLS LAST
                                  LIMIT 1
                                ),
                                casestatus_lu AS (
                                  SELECT (x ->> 'id') AS id, (x ->> 'caseStatus') AS casestatus_name
                                  FROM admin
                                  CROSS JOIN LATERAL jsonb_array_elements(admin.j -> 'casestatus') x
                                ),
                                casepriority_lu AS (
                                  SELECT (x ->> 'id') AS id, (x ->> 'casePriority') AS casepriority_name
                                  FROM admin
                                  CROSS JOIN LATERAL jsonb_array_elements(admin.j -> 'casepriority') x
                                )
                                SELECT
                                  ch.casenumber                                  AS ""CaseNumber"",
                                  ch.memberdetailid                              AS ""MemberDetailId"",
                                  ch.casetype::text                              AS ""CaseType"",
                                  cct.casetemplatename                           AS ""CaseTypeText"",
                                  concat_ws(' ', md.firstname, md.lastname)      AS ""MemberName"",
                                  md.memberid                                    AS ""MemberId"",
                                  su.username                                    AS ""CreatedByUserName"",
                                  ch.createdby                                   AS ""CreatedBy"",
                                  ch.createdon                                   AS ""CreatedOn"",
                                  cd.caselevelid                                 AS ""CaseLevelId"",
                                  (cd.jsondata::jsonb ->> 'Case_Overview_casePriority') AS ""CasePriority"",
                                  COALESCE(cp.casepriority_name,
                                           (cd.jsondata::jsonb ->> 'Case_Overview_casePriority')) AS ""CasePriorityText"",
                                  NULLIF(cd.jsondata::jsonb ->> 'Case_Overview_receivedDateTime','')::timestamptz
                                                                                AS ""ReceivedDateTime"",
                                  (cd.jsondata::jsonb ->> 'Case_Status_Details_caseStatus') AS ""CaseStatusId"",
                                  COALESCE(cs.casestatus_name,
                                           (cd.jsondata::jsonb ->> 'Case_Status_Details_caseStatus')) AS ""CaseStatusText"",
                                  COALESCE(cd.updatedon, cd.createdon)            AS ""LastDetailOn""
                                FROM caseheader ch
                                JOIN LATERAL (
                                  SELECT d.*
                                  FROM casedetail d
                                  WHERE d.caseheaderid = ch.caseheaderid
                                  ORDER BY d.caselevelid DESC NULLS LAST,
                                           COALESCE(d.updatedon, d.createdon) DESC NULLS LAST,
                                           d.casedetailid DESC
                                  LIMIT 1
                                ) cd ON TRUE
                                JOIN memberdetails md ON md.memberdetailsid = ch.memberdetailid
                                JOIN securityuser  su ON su.userid = ch.createdby
                                JOIN cfgcasetemplate cct ON cct.casetemplateid = ch.casetype::int
                                LEFT JOIN casestatus_lu  cs ON cs.id = (cd.jsondata::jsonb ->> 'Case_Status_Details_caseStatus')
                                LEFT JOIN casepriority_lu cp ON cp.id = (cd.jsondata::jsonb ->> 'Case_Overview_casePriority')
                                WHERE ch.memberdetailid = @memberId
                                ORDER BY COALESCE(cd.updatedon, cd.createdon) DESC NULLS LAST;
                                ";

            await using var conn = new NpgsqlConnection(_connStr);
            var rows = await conn.QueryAsync<AgCaseRow>(new CommandDefinition(sql, new { memberId = memberDetailId }, cancellationToken: ct));
            //var cmd = new CommandDefinition(sql, cancellationToken: ct);

            //var rows = await conn.QueryAsync<AgCaseRow>(cmd);
            return rows.AsList();
        }
    }

    // ICaseNotesRepository methods would go here************/
    public class CaseNotesRepository : ICaseNotesRepository
    {
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        private readonly string _connStr;

        public CaseNotesRepository(IConfiguration configuration)
        {
            _connStr = configuration.GetConnectionString("DefaultConnection");
        }

        private NpgsqlConnection CreateConn() => new NpgsqlConnection(_connStr);


        public async Task<CaseNotesTemplateResponse?> GetCaseNotesTemplateAsync(int caseTemplateId, CancellationToken ct = default)
        {
            const string sql = @"
                SELECT jsonb_path_query_first(
                         ct.jsoncontent::jsonb,
                         '$.** ? (@.sectionName == ""Case Notes"")'
                       ) AS section
                FROM cfgcasetemplate ct
                WHERE ct.casetemplateid = @caseTemplateId;";

            await using var conn = CreateConn();

            var sectionJson = await conn.ExecuteScalarAsync<string>(
                new CommandDefinition(sql, new { caseTemplateId }, cancellationToken: ct)
            );

            if (string.IsNullOrWhiteSpace(sectionJson))
                return null;

            // Parse to JsonElement so you can send it straight to UI or map it later
            using var doc = JsonDocument.Parse(sectionJson);

            return new CaseNotesTemplateResponse
            {
                CaseTemplateId = caseTemplateId,
                SectionName = "Case Notes",
                Section = doc.RootElement.Clone()
            };
        }

        public async Task<IReadOnlyList<CaseNoteDto>> GetNotesAsync(int caseHeaderId, int levelId, CancellationToken ct = default)
        {
            const string sql = @"
                        SELECT COALESCE(
                          (
                            SELECT jsonb_agg(n ORDER BY (n->>'createdOn')::timestamptz DESC)
                            FROM jsonb_array_elements(COALESCE(cd.jsondata->'Case_Notes_caseNotesGrid','[]'::jsonb)) n
                            WHERE n->>'deletedBy' IS NULL
                          ),
                          '[]'::jsonb
                        ) AS notes
                        FROM casedetail cd
                        WHERE cd.caseheaderid = @caseHeaderId
                          AND cd.caselevelid      = @levelId
                          ";

            await using var conn = CreateConn();
            var notesJson = await conn.ExecuteScalarAsync<string>(
                new CommandDefinition(sql, new { caseHeaderId, levelId }, cancellationToken: ct)
            );

            var list = JsonSerializer.Deserialize<List<CaseNoteDto>>(notesJson ?? "[]", JsonOpts) ?? new List<CaseNoteDto>();
            return list;
        }

        public async Task<Guid> InsertNoteAsync(
             int caseHeaderId,
             int levelId,
             CreateCaseNoteRequest req,
             int userId,
             CancellationToken ct = default)
        {
            var noteId = Guid.NewGuid();
            var now = DateTime.UtcNow;

            var newNote = new CaseNoteDto
            {
                NoteId = noteId,
                NoteText = req.NoteText ?? "",
                NoteLevel = req.NoteLevel,
                NoteType = req.NoteType,
                CaseAlertNote = req.CaseAlertNote,
                CreatedBy = userId,
                CreatedOn = now,
                UpdatedBy = null,
                UpdatedOn = null,
                DeletedBy = null,
                DeletedOn = null
            };

            var newNoteJson = JsonSerializer.Serialize(newNote, JsonOpts);

            const string sql = @"
                UPDATE casedetail
                SET jsondata =
                  jsonb_set(
                    COALESCE(jsondata, '{}'::jsonb),
                    '{Case_Notes_caseNotesGrid}',
                    COALESCE(jsondata->'Case_Notes_caseNotesGrid','[]'::jsonb) || jsonb_build_array(@newNote::jsonb),
                    true
                  )
                WHERE caseheaderid = @caseHeaderId
                  AND caselevelid  = @levelId;
                ";

            await using var conn = CreateConn();

            // Optional but recommended for concurrent inserts
            await LockCaseDetailRow(conn, caseHeaderId, levelId, ct);

            var p = new DynamicParameters();
            p.Add("caseHeaderId", caseHeaderId);
            p.Add("levelId", levelId);
            p.Add("newNote", newNoteJson, DbType.String);

            var rows = await conn.ExecuteAsync(new CommandDefinition(sql, p, cancellationToken: ct));

            if (rows == 0)
                throw new InvalidOperationException($"casedetail row not found for caseHeaderId={caseHeaderId}, levelId={levelId}");

            return noteId;
        }

        public async Task<bool> UpdateNoteAsync(int caseHeaderId, int levelId, Guid noteId, UpdateCaseNoteRequest req, int userId, CancellationToken ct = default)
        {
            // Build patch JSON containing ONLY provided fields
            var patch = new Dictionary<string, object?>();

            if (req.NoteText is not null) patch["noteText"] = req.NoteText;
            if (req.NoteLevel.HasValue) patch["noteLevel"] = req.NoteLevel.Value;
            if (req.NoteType.HasValue) patch["noteType"] = req.NoteType.Value;
            if (req.CaseAlertNote.HasValue) patch["caseAlertNote"] = req.CaseAlertNote.Value;

            // If nothing to update, return false
            if (patch.Count == 0) return false;

            var patchJson = JsonSerializer.Serialize(patch, JsonOpts);

            const string sql = @"
                    UPDATE casedetail cd
                        SET jsondata = jsonb_set(
                          COALESCE(cd.jsondata, '{}'::jsonb),
                          '{Case_Notes_caseNotesGrid}',
                          (
                            SELECT COALESCE(
                              jsonb_agg(
                                CASE
                                  WHEN n->>'noteId' = @noteId
                                    THEN (
                                      n
                                      || COALESCE(@patch::jsonb, '{}'::jsonb)
                                      || jsonb_build_object(
                                           'updatedOn', to_jsonb(NOW()),
                                           'updatedBy', to_jsonb(@userId)
                                         )
                                    )
                                  ELSE n
                                END
                              ),
                              '[]'::jsonb
                            )
                            FROM jsonb_array_elements(COALESCE(cd.jsondata->'Case_Notes_caseNotesGrid','[]'::jsonb)) n
                          ),
                          true
                        )
                        WHERE cd.caseheaderid = @caseHeaderId
                          AND cd.caselevelid  = @levelId;";

            await using var conn = CreateConn();

            await LockCaseDetailRow(conn, caseHeaderId, levelId, ct);


            var rows = await conn.ExecuteAsync(new CommandDefinition(sql, new
            {
                caseHeaderId,
                levelId,
                noteId = noteId.ToString(),
                patch = patchJson,
                userId
            }, cancellationToken: ct));

            // rows will be 1 if the casedetail row updated; doesn’t guarantee noteId existed.
            // If you need strict "note must exist" semantics, we can add a noteId existence check.
            return rows > 0;
        }

        public async Task<bool> SoftDeleteNoteAsync(int caseHeaderId, int levelId, Guid noteId, int userId, CancellationToken ct = default)
        {
            const string sql = @"
                    UPDATE casedetail cd
                        SET jsondata = jsonb_set(
                          COALESCE(cd.jsondata, '{}'::jsonb),
                          '{Case_Notes_caseNotesGrid}',
                          (
                            SELECT COALESCE(
                              jsonb_agg(
                                CASE
                                  WHEN n->>'noteId' = @noteId::text
                                    THEN n || jsonb_build_object(
                                              'deletedBy', to_jsonb(@userId),
                                              'deletedOn', to_jsonb(NOW())
                                            )
                                  ELSE n
                                END
                              ),
                              '[]'::jsonb
                            )
                            FROM jsonb_array_elements(COALESCE(cd.jsondata->'Case_Notes_caseNotesGrid','[]'::jsonb)) n
                          ),
                          true
                        )
                        WHERE cd.caseheaderid = @caseHeaderId
                          AND cd.caselevelid  = @levelId;
                        ";

            await using var conn = CreateConn();

            await LockCaseDetailRow(conn, caseHeaderId, levelId, ct);

            var rows = await conn.ExecuteAsync(new CommandDefinition(sql, new
            {
                caseHeaderId,
                levelId,
                noteId = noteId.ToString(),
                userId
            }, cancellationToken: ct));

            return rows > 0;
        }

        /// <summary>
        /// Row lock to avoid lost updates when multiple users add/update notes simultaneously.
        /// This is optional but strongly recommended.
        /// </summary>
        private static async Task LockCaseDetailRow(NpgsqlConnection conn, int caseHeaderId, int levelId, CancellationToken ct)
        {
            // Ensure connection open
            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync(ct);

            const string lockSql = @"
                    SELECT 1
                    FROM casedetail
                    WHERE caseheaderid = @caseHeaderId
                        AND caselevelid      = @levelId
                    FOR UPDATE;";

            // If row doesn't exist, FOR UPDATE returns 0 rows and that's okay (upsert will insert).
            await conn.ExecuteAsync(new CommandDefinition(lockSql, new { caseHeaderId, levelId }, cancellationToken: ct));
        }
    }

    ///************ ICaseDocumentRepository methods end here */
    public class CaseDocumentsRepository : ICaseDocumentsRepository
    {
        private readonly string _connStr;

        private const string DocsKey = "Case_Documents_caseDocumentsGrid";

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public CaseDocumentsRepository(IConfiguration configuration)
        {
            _connStr = configuration.GetConnectionString("DefaultConnection");
        }

        private NpgsqlConnection CreateConn() => new NpgsqlConnection(_connStr);

        public async Task<CaseDocumentsTemplateResponse?> GetCaseDocumentsTemplateAsync(int caseTemplateId, CancellationToken ct = default)
        {
            const string sql = @"
                SELECT jsonb_path_query_first(
                         ct.jsoncontent::jsonb,
                         '$.** ? (@.sectionName == ""Case Documents"")'
                       ) AS section
                FROM cfgcasetemplate ct
                WHERE ct.casetemplateid = @caseTemplateId;";

            await using var conn = CreateConn();
            var sectionJson = await conn.ExecuteScalarAsync<string>(
                new CommandDefinition(sql, new { caseTemplateId }, cancellationToken: ct)
            );

            if (string.IsNullOrWhiteSpace(sectionJson))
                return null;

            using var doc = JsonDocument.Parse(sectionJson);

            return new CaseDocumentsTemplateResponse
            {
                CaseTemplateId = caseTemplateId,
                SectionName = "Case Documents",
                Section = doc.RootElement.Clone()
            };
        }

        public async Task<IReadOnlyList<CaseDocumentDto>> GetDocumentsAsync(int caseHeaderId, int levelId, CancellationToken ct = default)
        {
            var sql = $@"
                SELECT COALESCE(
                  (
                    SELECT jsonb_agg(d ORDER BY (d->>'createdOn')::timestamptz DESC)
                    FROM jsonb_array_elements(COALESCE(cd.jsondata->'{DocsKey}','[]'::jsonb)) d
                    WHERE d->>'deletedBy' IS NULL
                  ),
                  '[]'::jsonb
                ) AS documents
                FROM casedetail cd
                WHERE cd.caseheaderid = @caseHeaderId
                  AND cd.caselevelid  = @levelId
                  AND cd.deletedon IS NULL;";

            await using var conn = CreateConn();

            var docsJson = await conn.ExecuteScalarAsync<string>(
                new CommandDefinition(sql, new { caseHeaderId, levelId }, cancellationToken: ct)
            );

            return JsonSerializer.Deserialize<List<CaseDocumentDto>>(docsJson ?? "[]", JsonOpts)
                   ?? new List<CaseDocumentDto>();
        }

        public async Task<Guid> InsertDocumentAsync(int caseHeaderId, int levelId, CreateCaseDocumentRequest req, int userId, CancellationToken ct = default)
        {
            var documentId = Guid.NewGuid();
            var now = DateTime.UtcNow;

            var newDoc = new CaseDocumentDto
            {
                DocumentId = documentId,
                DocumentType = req.DocumentType,
                DocumentLevel = req.DocumentLevel,
                DocumentDescription = req.DocumentDescription ?? "",
                FileNames = req.FileNames ?? new List<string>(),

                CreatedBy = userId,
                CreatedOn = now,
                UpdatedBy = null,
                UpdatedOn = null,
                DeletedBy = null,
                DeletedOn = null
            };

            var newDocJson = JsonSerializer.Serialize(newDoc, JsonOpts);

            var sql = $@"
                UPDATE casedetail cd
                SET jsondata =
                  jsonb_set(
                    COALESCE(cd.jsondata,'{{}}'::jsonb),
                    '{{{DocsKey}}}',
                    COALESCE(cd.jsondata->'{DocsKey}','[]'::jsonb) || jsonb_build_array(@doc::jsonb),
                    true
                  ),
                  updatedon = NOW(),
                  updatedby = @userId
                WHERE cd.caseheaderid = @caseHeaderId
                  AND cd.caselevelid  = @levelId
                  AND cd.deletedon IS NULL;";

            await using var conn = CreateConn();

            // recommended: lock row for concurrent writers
            var exists = await LockCaseDetailRow(conn, caseHeaderId, levelId, ct);
            if (!exists) throw new InvalidOperationException($"casedetail not found for caseHeaderId={caseHeaderId}, levelId={levelId}");

            var rows = await conn.ExecuteAsync(new CommandDefinition(sql, new
            {
                caseHeaderId,
                levelId,
                doc = newDocJson,
                userId
            }, cancellationToken: ct));

            if (rows == 0) throw new InvalidOperationException("No rows updated while inserting document.");

            return documentId;
        }

        public async Task<bool> UpdateDocumentAsync(int caseHeaderId, int levelId, Guid documentId, UpdateCaseDocumentRequest req, int userId, CancellationToken ct = default)
        {
            // build a camelCase patch JSON (matches stored JSON)
            var patch = new Dictionary<string, object?>();

            if (req.DocumentType.HasValue) patch["documentType"] = req.DocumentType.Value;
            if (req.DocumentLevel.HasValue) patch["documentLevel"] = req.DocumentLevel.Value;
            if (req.DocumentDescription != null) patch["documentDescription"] = req.DocumentDescription;
            if (req.FileNames != null) patch["fileNames"] = req.FileNames;

            if (patch.Count == 0) return false;

            var patchJson = JsonSerializer.Serialize(patch, JsonOpts);

            var sql = $@"
                UPDATE casedetail cd
                SET jsondata = jsonb_set(
                  COALESCE(cd.jsondata, '{{}}'::jsonb),
                  '{{{DocsKey}}}',
                  (
                    SELECT COALESCE(
                      jsonb_agg(
                        CASE
                          WHEN d->>'documentId' = @documentId::text THEN
                            (d || @patch::jsonb || jsonb_build_object('updatedOn', to_jsonb(NOW()), 'updatedBy', to_jsonb(@userId)))
                          ELSE d
                        END
                      ),
                      '[]'::jsonb
                    )
                    FROM jsonb_array_elements(COALESCE(cd.jsondata->'{DocsKey}','[]'::jsonb)) d
                  ),
                  true
                ),
                updatedon = NOW(),
                updatedby = @userId
                WHERE cd.caseheaderid = @caseHeaderId
                  AND cd.caselevelid  = @levelId
                  AND cd.deletedon IS NULL;";

            await using var conn = CreateConn();
            var exists = await LockCaseDetailRow(conn, caseHeaderId, levelId, ct);
            if (!exists) return false;

            var rows = await conn.ExecuteAsync(new CommandDefinition(sql, new
            {
                caseHeaderId,
                levelId,
                documentId,
                patch = patchJson,
                userId
            }, cancellationToken: ct));

            return rows > 0;
        }

        public async Task<bool> SoftDeleteDocumentAsync(int caseHeaderId, int levelId, Guid documentId, int userId, CancellationToken ct = default)
        {
            var sql = $@"
                UPDATE casedetail cd
                SET jsondata = jsonb_set(
                  COALESCE(cd.jsondata, '{{}}'::jsonb),
                  '{{{DocsKey}}}',
                  (
                    SELECT COALESCE(
                      jsonb_agg(
                        CASE
                          WHEN d->>'documentId' = @documentId::text THEN
                            (d || jsonb_build_object('deletedBy', to_jsonb(@userId), 'deletedOn', to_jsonb(NOW())))
                          ELSE d
                        END
                      ),
                      '[]'::jsonb
                    )
                    FROM jsonb_array_elements(COALESCE(cd.jsondata->'{DocsKey}','[]'::jsonb)) d
                  ),
                  true
                ),
                updatedon = NOW(),
                updatedby = @userId
                WHERE cd.caseheaderid = @caseHeaderId
                  AND cd.caselevelid  = @levelId
                  AND cd.deletedon IS NULL;";

            await using var conn = CreateConn();
            var exists = await LockCaseDetailRow(conn, caseHeaderId, levelId, ct);
            if (!exists) return false;

            var rows = await conn.ExecuteAsync(new CommandDefinition(sql, new
            {
                caseHeaderId,
                levelId,
                documentId,
                userId
            }, cancellationToken: ct));

            return rows > 0;
        }

        private static async Task<bool> LockCaseDetailRow(NpgsqlConnection conn, int caseHeaderId, int levelId, CancellationToken ct)
        {
            const string lockSql = @"
                SELECT 1
                FROM casedetail
                WHERE caseheaderid = @caseHeaderId
                  AND caselevelid  = @levelId
                  AND deletedon IS NULL
                FOR UPDATE;";

            var found = await conn.ExecuteScalarAsync<int?>(
                new CommandDefinition(lockSql, new { caseHeaderId, levelId }, cancellationToken: ct)
            );

            return found.HasValue;
        }
    }
}
