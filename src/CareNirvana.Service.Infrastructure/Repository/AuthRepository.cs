using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Data;
using System.Text.Json;

namespace CareNirvana.Service.Infrastructure.Repository
{
    public class AuthRepository : IAuthRepository
    {
        private readonly string _connStr;

        public AuthRepository(IConfiguration configuration)
        {
            _connStr = configuration.GetConnectionString("DefaultConnection");
        }

        private NpgsqlConnection CreateConn() => new NpgsqlConnection(_connStr);

        public async Task<AuthDetailRow?> GetAuthByNumberAsync(string authNumber, bool includeDeleted = false)
        {
            const string sql = @"
                with auth_status as (
                  select
                      (s->>'id')::int as AuthStatusId,
                      s->>'authStatus' as AuthStatusText,
                      coalesce((s->>'activeFlag')::boolean, false) as ActiveFlag
                  from cfgadmindata c
                  cross join lateral jsonb_array_elements(c.jsoncontent->'authstatus') s
                  where c.module = 'UM'
                )
                select
                    a.authdetailid as AuthDetailId,
                    a.authnumber as AuthNumber,
                    a.authtypeid as AuthTypeId,
                    at.authtemplatename as AuthTemplateName,
                    a.memberdetailsid as MemberDetailsId,
                    a.authduedate as AuthDueDate,
                    a.nextreviewdate as NextReviewDate,
                    a.treatementtype as TreatementType,
                    a.data::text as DataJson,
                    a.createdon as CreatedOn,
                    a.createdby as CreatedBy,
                    a.updatedon as UpdatedOn,
                    a.updatedby as UpdatedBy,
                    a.deletedon as DeletedOn,
                    a.deletedby as DeletedBy,
                    a.authclassid as AuthClassId,
                    a.authassignedto as AuthAssignedTo,
                    a.authstatus as AuthStatus,
                    st.AuthStatusText as AuthStatusText,
                    su.username as CreatedByUserName,
                    md.memberid as MemberId,
                    (coalesce(md.firstname,'') ||
                      case when md.lastname is null or md.lastname = '' then '' else ' ' || md.lastname end
                    ) as MemberName,
                    coalesce(wg.is_assigned, false) as IsWorkgroupAssigned,
                    (coalesce(wg.is_assigned, false) = true and coalesce(wg.any_accept, false) = false) as IsWorkgroupPending


                from authdetail a
                left join cfgauthtemplate at on at.authtemplateid = a.authtypeid
                left join securityuser su on su.userid = a.createdby
                left join memberdetails md on md.memberdetailsid = a.memberdetailsid
                left join auth_status st
                       on st.AuthStatusId = a.authstatus
                      and st.ActiveFlag = true
               left join lateral (
                    select
                        (count(*) > 0) as is_assigned,
                        array_remove(array_agg(awg.workgroupworkbasketid order by awg.createdon desc), null) as wgwb_ids,
                        bool_or(
                            exists (
                                select 1
                                from authworkgroupaction awa
                                where awa.authworkgroupid = awg.authworkgroupid
                                  and awa.activeflag = true
                                  and upper(awa.actiontype) = 'ACCEPT'
                            )
                        ) as any_accept
                    from authworkgroup awg
                    where awg.authdetailid = a.authdetailid
                      and awg.requesttype = 'AUTH'
                      and awg.activeflag = true
                ) wg on true

                where a.authnumber = @authNumber
                  and (@includeDeleted = true or a.deletedon is null)
                order by a.createdon desc;";

            await using var conn = CreateConn();
            return await conn.QueryFirstOrDefaultAsync<AuthDetailRow>(sql, new { authNumber, includeDeleted });
        }

        public async Task<AuthDetailRow?> GetAuthByIdAsync(long authDetailId, bool includeDeleted = false)
        {
            const string sql = @"
                with auth_status as (
                  select
                      (s->>'id')::int as AuthStatusId,
                      s->>'authStatus' as AuthStatusText,
                      coalesce((s->>'activeFlag')::boolean, false) as ActiveFlag
                  from cfgadmindata c
                  cross join lateral jsonb_array_elements(c.jsoncontent->'authstatus') s
                  where c.module = 'UM'
                )
                select
                    a.authdetailid as AuthDetailId,
                    a.authnumber as AuthNumber,
                    a.authtypeid as AuthTypeId,
                    at.authtemplatename as AuthTemplateName,
                    a.memberdetailsid as MemberDetailsId,
                    a.authduedate as AuthDueDate,
                    a.nextreviewdate as NextReviewDate,
                    a.treatementtype as TreatementType,
                    a.data::text as DataJson,
                    a.createdon as CreatedOn,
                    a.createdby as CreatedBy,
                    a.updatedon as UpdatedOn,
                    a.updatedby as UpdatedBy,
                    a.deletedon as DeletedOn,
                    a.deletedby as DeletedBy,
                    a.authclassid as AuthClassId,
                    a.authassignedto as AuthAssignedTo,
                    a.authstatus as AuthStatus,
                    st.AuthStatusText as AuthStatusText,
                    su.username as CreatedByUserName,
                    md.memberid as MemberId,
                    (coalesce(md.firstname,'') ||
                      case when md.lastname is null or md.lastname = '' then '' else ' ' || md.lastname end
                    ) as MemberName,
                    coalesce(wg.is_assigned, false) as IsWorkgroupAssigned,
                    (coalesce(wg.is_assigned, false) = true and coalesce(wg.any_accept, false) = false) as IsWorkgroupPending

                from authdetail a
                left join cfgauthtemplate at on at.authtemplateid = a.authtypeid
                left join securityuser su on su.userid = a.createdby
                left join memberdetails md on md.memberdetailsid = a.memberdetailsid
                left join auth_status st
                       on st.AuthStatusId = a.authstatus
                      and st.ActiveFlag = true
               left join lateral (
                    select
                        (count(*) > 0) as is_assigned,
                        array_remove(array_agg(awg.workgroupworkbasketid order by awg.createdon desc), null) as wgwb_ids,
                        bool_or(
                            exists (
                                select 1
                                from authworkgroupaction awa
                                where awa.authworkgroupid = awg.authworkgroupid
                                  and awa.activeflag = true
                                  and upper(awa.actiontype) = 'ACCEPT'
                            )
                        ) as any_accept
                    from authworkgroup awg
                    where awg.authdetailid = a.authdetailid
                      and awg.requesttype = 'AUTH'
                      and awg.activeflag = true
                ) wg on true      


                where a.authdetailid = @authDetailId
                  and (@includeDeleted = true or a.deletedon is null)
                order by a.createdon desc;";


            await using var conn = CreateConn();
            return await conn.QueryFirstOrDefaultAsync<AuthDetailRow>(sql, new { authDetailId, includeDeleted });
        }

        public async Task<List<AuthDetailRow>> GetAuthsByMemberAsync(int memberDetailsId, bool includeDeleted = false)
        {
            const string sql = @"
                WITH admin AS (
                      SELECT jsoncontent::jsonb AS j
                      FROM cfgadmindata
                      WHERE module = 'UM'
                      ORDER BY COALESCE(updatedon, createdon) DESC NULLS LAST
                      LIMIT 1
                    ),
                    auth_status AS (
                      SELECT
                        (s->>'id')::int AS authstatusid,
                        s->>'authStatus' AS authstatustext,
                        COALESCE((s->>'activeFlag')::boolean, false) AS activeflag
                      FROM admin
                      CROSS JOIN LATERAL jsonb_array_elements(COALESCE(admin.j->'authstatus', '[]'::jsonb)) s
                    ),
                    auth_treatment AS (
                      SELECT
                        (s->>'id')::int AS treatmentid,
                        s->>'treatmentType' AS treatmenttext
                      FROM admin
                      CROSS JOIN LATERAL jsonb_array_elements(COALESCE(admin.j->'treatmenttype', '[]'::jsonb)) s
                    ),
                    auth_priority AS (
                      SELECT
                        (s->>'id')::int AS priorityid,
                        s->>'requestPriority' AS requestprioritytext
                      FROM admin
                      CROSS JOIN LATERAL jsonb_array_elements(COALESCE(admin.j->'requestpriority', '[]'::jsonb)) s
                    )
                    SELECT
                      a.authdetailid AS ""AuthDetailId"",
                      a.authnumber AS ""AuthNumber"",
                      a.authtypeid AS ""AuthTypeId"",
                      tmpl.authtemplatename AS ""AuthTemplateName"",
                      a.memberdetailsid AS ""MemberDetailsId"",

                      COALESCE(a.authduedate, a.createdon + interval '10 days') AS ""AuthDueDate"",

                      COALESCE(ar.nextreviewdate, a.nextreviewdate) AS ""NextReviewDate"",

                      tt.treatmenttext AS ""TreatementType"",
                      ap.requestprioritytext AS ""RequestPriority"",
                      NULL::text AS ""DataJson"",
                      a.createdon AS ""CreatedOn"",
                      a.createdby AS ""CreatedBy"",
                      a.updatedon AS ""UpdatedOn"",
                      a.updatedby AS ""UpdatedBy"",
                      a.deletedon AS ""DeletedOn"",
                      a.deletedby AS ""DeletedBy"",
                      a.authclassid AS ""AuthClassId"",
                      a.authassignedto AS ""AuthAssignedTo"",
                      a.authstatus AS ""AuthStatus"",
                      st.authstatustext AS ""AuthStatusText"",
                      su.username AS ""CreatedByUserName"",
                      md.memberid AS ""MemberId"",
                      (COALESCE(md.firstname,'') ||
                        CASE
                          WHEN md.lastname IS NULL OR md.lastname = '' THEN ''
                          ELSE ' ' || md.lastname
                        END
                      ) AS ""MemberName"",
                      COALESCE(wg.is_assigned, false) AS ""IsWorkgroupAssigned"",
                      (COALESCE(wg.is_assigned, false) = true AND COALESCE(wg.any_accept, false) = false) AS ""IsWorkgroupPending""

                    FROM authdetail a
                    LEFT JOIN cfgauthtemplate tmpl ON tmpl.authtemplateid = a.authtypeid
                    LEFT JOIN securityuser su ON su.userid = a.createdby
                    LEFT JOIN memberdetails md ON md.memberdetailsid = a.memberdetailsid
                    LEFT JOIN auth_status st
                      ON st.authstatusid = a.authstatus
                     AND st.activeflag = true
                    LEFT JOIN auth_treatment tt
                      ON tt.treatmentid = a.treatementtype::int

                    LEFT JOIN auth_priority ap ON ap.priorityid::text = (a.data::jsonb ->> 'requestSent')
                    LEFT JOIN LATERAL (
                      SELECT MIN(aa.followupdatetime) AS nextreviewdate
                      FROM authactivity aa
                      WHERE aa.authdetailid = a.authdetailid
                        AND aa.referto IS NOT NULL
                        AND aa.followupdatetime IS NOT NULL
                    ) ar ON true

                    LEFT JOIN LATERAL (
                      SELECT
                        (COUNT(*) > 0) AS is_assigned,
                        array_remove(array_agg(awg.workgroupworkbasketid ORDER BY awg.createdon DESC), NULL) AS wgwb_ids,
                        bool_or(
                          EXISTS (
                            SELECT 1
                            FROM authworkgroupaction awa
                            WHERE awa.authworkgroupid = awg.authworkgroupid
                              AND awa.activeflag = true
                              AND upper(awa.actiontype) = 'ACCEPT'
                          )
                        ) AS any_accept
                      FROM authworkgroup awg
                      WHERE awg.authdetailid = a.authdetailid
                        AND awg.requesttype = 'AUTH'
                        AND awg.activeflag = true
                    ) wg ON true

                where a.memberdetailsid = @memberDetailsId
                  and (@includeDeleted = true or a.deletedon is null)
                order by a.createdon desc;";





            await using var conn = CreateConn();
            var rows = await conn.QueryAsync<AuthDetailRow>(sql, new { memberDetailsId, includeDeleted });
            return rows.AsList();
        }

        public async Task<long> CreateAuthAsync(CreateAuthRequest req, int userId)
        {
            var requestType = (req.RequestType ?? "AUTH").Trim().ToUpperInvariant();
            var ids = NormalizeWgWbIds(req.WorkgroupWorkbasketIds, req.WorkgroupWorkbasketId);
            var hasWgWb = ids.Length > 0;

            // AUTH rule: selected WG/WB => assignedto NULL, else assignedto createdby
            int? authAssignedTo;
            if (requestType == "AUTH")
                authAssignedTo = hasWgWb ? (int?)null : userId;
            else
                authAssignedTo = req.AuthAssignedTo; // don’t force for ACTIVITY here

            const string sql = @"
        insert into authdetail
            (authnumber, authtypeid, memberdetailsid, authduedate, nextreviewdate, treatementtype,
             data, createdon, createdby, authclassid, authassignedto, authstatus)
        values
            (@authNumber, @authTypeId, @memberDetailsId, @authDueDate, @nextReviewDate, @treatementType,
             @jsonData::jsonb, now(), @userId, @authClassId, @authAssignedTo, @authStatus)
        returning authdetailid;";

            await using var conn = CreateConn();
            var authDetailId = await conn.ExecuteScalarAsync<long>(sql, new
            {
                authNumber = req.AuthNumber,
                authTypeId = req.AuthTypeId,
                memberDetailsId = req.MemberDetailsId,
                authDueDate = req.AuthDueDate,
                nextReviewDate = req.NextReviewDate,
                treatementType = req.TreatementType,
                jsonData = req.JsonData,
                authClassId = req.AuthClassId,
                authAssignedTo,
                authStatus = req.AuthStatus,
                userId
            });

            // Persist authworkgroup rows (call even if empty for AUTH if you want central behavior)
            // Here we call only if ids exist OR if requestType is AUTH (so empty clears/deactivates and sets assignedTo=userId).
            if (requestType == "AUTH" || hasWgWb)
            {
                await SaveAuthWorkgroupsAsync(new SaveAuthWorkgroupsRequest
                {
                    RequestType = requestType,
                    AuthDetailId = authDetailId,
                    AuthActivityId = req.AuthActivityId,
                    WorkgroupWorkbasketIds = ids,
                    GroupStatusId = req.GroupStatusId
                }, userId);
            }

            return authDetailId;
        }



        public async Task UpdateAuthAsync(long authDetailId, UpdateAuthRequest req, int userId)
        {
            var requestType = (req.RequestType ?? "AUTH").Trim().ToUpperInvariant();
            var ids = NormalizeWgWbIds(req.WorkgroupWorkbasketIds, req.WorkgroupWorkbasketId);
            var hasWgWb = ids.Length > 0;

            // AUTH assignment rules
            var clearAuthAssignedTo = (requestType == "AUTH" && hasWgWb);
            var setAssignedToCreator = (requestType == "AUTH" && !hasWgWb);

            const string sql = @"
        update authdetail
        set authtypeid     = coalesce(@authTypeId, authtypeid),
            authduedate     = coalesce(@authDueDate, authduedate),
            nextreviewdate  = coalesce(@nextReviewDate, nextreviewdate),
            treatementtype  = coalesce(@treatementType, treatementtype),
            authclassid     = coalesce(@authClassId, authclassid),

            authassignedto  = case
                                when @clearAuthAssignedTo = true then null
                                when @setAssignedToCreator = true then @userId
                                when @authAssignedTo is null then authassignedto
                                else @authAssignedTo
                              end,

            authstatus      = coalesce(@authStatus, authstatus),
            data            = case when @jsonData is null then data else @jsonData::jsonb end,
            updatedon       = now(),
            updatedby       = @userId
        where authdetailid = @authDetailId
          and deletedon is null;";

            await using var conn = CreateConn();
            await conn.ExecuteAsync(sql, new
            {
                authDetailId,
                authTypeId = req.AuthTypeId,
                authDueDate = req.AuthDueDate,
                nextReviewDate = req.NextReviewDate,
                treatementType = req.TreatementType,
                authClassId = req.AuthClassId,

                authAssignedTo = req.AuthAssignedTo,
                clearAuthAssignedTo,
                setAssignedToCreator,

                authStatus = req.AuthStatus,
                jsonData = req.JsonData,
                userId
            });

            // Keep authworkgroup rows in sync with selection for AUTH and ACTIVITY
            await SaveAuthWorkgroupsAsync(new SaveAuthWorkgroupsRequest
            {
                RequestType = requestType,
                AuthDetailId = authDetailId,
                AuthActivityId = req.AuthActivityId,
                WorkgroupWorkbasketIds = ids,
                GroupStatusId = req.GroupStatusId
            }, userId);
        }



        public async Task SoftDeleteAuthAsync(long authDetailId, int userId)
        {
            const string sql = @"
                update authdetail
                set deletedon = now(),
                    deletedby = @userId
                where authdetailid = @authDetailId
                  and deletedon is null;";

            await using var conn = CreateConn();
            await conn.ExecuteAsync(sql, new { authDetailId, userId });
        }

        public async Task<TemplateSectionsResponse?> GetDecisionTemplateAsync(int authTemplateId, CancellationToken ct = default)
        {
            const string sql = @"
                SELECT COALESCE(jsonb_agg(s.section), '[]'::jsonb)::text AS sections
                FROM cfgauthtemplate ct
                CROSS JOIN LATERAL (
                    SELECT jsonb_path_query(
                             ct.jsoncontent::jsonb,
                             '$.** ? (@.sectionName == $d1 || @.sectionName == $d2 || @.sectionName == $d3)',
                             jsonb_build_object(
                                'd1', to_jsonb(@d1),
                                'd2', to_jsonb(@d2),
                                'd3', to_jsonb(@d3)
                             )
                           ) AS section
                ) s
                WHERE ct.authtemplateid = @authTemplateId;";

            await using var conn = CreateConn();

            var sectionsJson = await conn.ExecuteScalarAsync<string>(
                new CommandDefinition(sql, new
                {
                    authTemplateId,
                    d1 = "Decision Details",
                    d2 = "Member Provider Decision Info",
                    d3 = "Decision Notes"
                }, cancellationToken: ct)
            );

            if (string.IsNullOrWhiteSpace(sectionsJson))
                return null;

            using var doc = JsonDocument.Parse(sectionsJson);

            return new TemplateSectionsResponse
            {
                CaseTemplateId = authTemplateId, // rename property if you want (see note below)
                GroupName = "Decision",
                Sections = doc.RootElement.Clone()
            };
        }

        private static string MapDecisionSectionToKey(string sectionName) => sectionName switch
        {
            "Decision Details" => "decisionDetails",
            "Member Provider Decision Info" => "memberProviderDecisionInfo",
            "Decision Notes" => "decisionNotes",
            _ => throw new ArgumentOutOfRangeException(nameof(sectionName), $"Unsupported section: {sectionName}")
        };

        public async Task<IReadOnlyList<DecisionSectionItemDto>> GetDecisionSectionItemsAsync(long authDetailId, string sectionName, CancellationToken ct = default)
        {
            var sectionKey = MapDecisionSectionToKey(sectionName);

            const string sql = @"
                select coalesce(
                  (
                    select jsonb_agg(n order by (n->>'createdOn')::timestamptz desc)
                    from jsonb_array_elements(coalesce(a.data->@sectionKey, '[]'::jsonb)) n
                    where n->>'deletedBy' is null
                  ),
                  '[]'::jsonb
                )::text as items
                from authdetail a
                where a.authdetailid = @authDetailId
                  and a.deletedon is null;";

            await using var conn = CreateConn();
            var itemsJson = await conn.ExecuteScalarAsync<string>(
                new CommandDefinition(sql, new { authDetailId, sectionKey }, cancellationToken: ct));

            return JsonSerializer.Deserialize<List<DecisionSectionItemDto>>(itemsJson ?? "[]", JsonOpts)
                   ?? new List<DecisionSectionItemDto>();
        }

        private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };


        public async Task<Guid> InsertDecisionSectionItemAsync(long authDetailId, string sectionName, CreateDecisionSectionItemRequest req, int userId, CancellationToken ct = default)
        {
            var sectionKey = MapDecisionSectionToKey(sectionName);
            var itemId = Guid.NewGuid();
            var path = new[] { sectionKey };
            var dataJson = req.Data.GetRawText();

            const string sql = @"
                update authdetail a
                set data = jsonb_set(
                    coalesce(a.data, '{}'::jsonb),
                    @path,
                    coalesce(a.data->@sectionKey, '[]'::jsonb) || jsonb_build_object(
                      'itemId', @itemId::text,
                      'data', @data::jsonb,
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
                sectionKey,
                path,
                itemId,
                data = dataJson,
                userId
            }, cancellationToken: ct));

            if (rows == 0) throw new InvalidOperationException("Auth not found or deleted.");
            return itemId;
        }

        public async Task<bool> UpdateDecisionSectionItemAsync(long authDetailId, string sectionName, Guid itemId, UpdateDecisionSectionItemRequest req, int userId, CancellationToken ct = default)
        {
            var sectionKey = MapDecisionSectionToKey(sectionName);
            var path = new[] { sectionKey };
            var dataJson = req.Data.HasValue ? req.Data.Value.GetRawText() : null;

            const string sql = @"
                update authdetail a
                set data = jsonb_set(
                    coalesce(a.data, '{}'::jsonb),
                    @path,
                    coalesce((
                      select jsonb_agg(
                        case
                          when n->>'itemId' = @itemId::text then
                            (n || jsonb_build_object(
                              'data', coalesce(@data::jsonb, n->'data'),
                              'updatedBy', @userId,
                              'updatedOn', now()
                            ))
                          else n
                        end
                        order by (n->>'createdOn')::timestamptz desc
                      )
                      from jsonb_array_elements(coalesce(a.data->@sectionKey, '[]'::jsonb)) n
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
                sectionKey,
                path,
                itemId,
                data = dataJson, // null allowed, cast in SQL is safe
                userId
            }, cancellationToken: ct));

            if (rows == 0) throw new InvalidOperationException("Auth not found or deleted.");
            return rows > 0;
        }

        public async Task<bool> SoftDeleteDecisionSectionItemAsync(long authDetailId, string sectionName, Guid itemId, int userId, CancellationToken ct = default)
        {
            var sectionKey = MapDecisionSectionToKey(sectionName);
            var path = new[] { sectionKey };

            const string sql = @"
                update authdetail a
                set data = jsonb_set(
                    coalesce(a.data, '{}'::jsonb),
                    @path,
                    coalesce((
                      select jsonb_agg(
                        case
                          when n->>'itemId' = @itemId::text then
                            (n || jsonb_build_object(
                              'deletedBy', @userId,
                              'deletedOn', now()
                            ))
                          else n
                        end
                        order by (n->>'createdOn')::timestamptz desc
                      )
                      from jsonb_array_elements(coalesce(a.data->@sectionKey, '[]'::jsonb)) n
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
                sectionKey,
                path,
                itemId,
                userId
            }, cancellationToken: ct));

            if (rows == 0) throw new InvalidOperationException("Auth not found or deleted.");
            return rows > 0;
        }

        public async Task SaveAuthWorkgroupsAsync(SaveAuthWorkgroupsRequest req, int userId)
        {
            var requestType = (req.RequestType ?? "AUTH").Trim().ToUpperInvariant();
            if (requestType != "AUTH" && requestType != "ACTIVITY")
                throw new ArgumentException("RequestType must be AUTH or ACTIVITY.");

            if (requestType == "ACTIVITY" && (!req.AuthActivityId.HasValue || req.AuthActivityId.Value <= 0))
                throw new ArgumentException("AuthActivityId is required when RequestType is ACTIVITY.");

            var ids = (req.WorkgroupWorkbasketIds ?? Array.Empty<int>())
                .Where(x => x > 0)
                .Distinct()
                .ToArray();

            await using var conn = CreateConn();
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            // AUTH rule: keep authassignedto aligned with selection
            if (requestType == "AUTH")
            {
                const string assignSql = @"
                    update authdetail
                    set authassignedto =
                            case when @hasIds = true then null else @userId end,
                        updatedon = now(),
                        updatedby = @userId
                    where authdetailid = @authDetailId
                      and deletedon is null;";

                await conn.ExecuteAsync(assignSql, new
                {
                    authDetailId = req.AuthDetailId,
                    hasIds = ids.Length > 0,
                    userId
                }, tx);
            }

            // Deactivate rows NOT in selected list (within same scope)
            const string deactivateSql = @"
                update authworkgroup
                set activeflag = false,
                    updatedon = now(),
                    updatedby = @userId
                where authdetailid = @authDetailId
                  and requesttype = @requestType
                  and activeflag = true
                  and (
                        (@requestType = 'AUTH' and authactivityid is null)
                     or (@requestType = 'ACTIVITY' and authactivityid = @authActivityId)
                  )
                  and (
                        @idsLen = 0
                        or workgroupworkbasketid <> all(@ids)
                  );";

            await conn.ExecuteAsync(deactivateSql, new
            {
                authDetailId = req.AuthDetailId,
                requestType,
                authActivityId = req.AuthActivityId,
                ids,
                idsLen = ids.Length,
                userId
            }, tx);

            // If no ids, we're done after deactivation (and AUTH assignment above)
            if (ids.Length == 0)
            {
                await tx.CommitAsync();
                return;
            }

            // Reactivate existing rows IN selected list (within same scope)
            const string activateSql = @"
                update authworkgroup
                set activeflag = true,
                    updatedon = now(),
                    updatedby = @userId
                where authdetailid = @authDetailId
                  and requesttype = @requestType
                  and (
                        (@requestType = 'AUTH' and authactivityid is null)
                     or (@requestType = 'ACTIVITY' and authactivityid = @authActivityId)
                  )
                  and workgroupworkbasketid = any(@ids);";

            await conn.ExecuteAsync(activateSql, new
            {
                authDetailId = req.AuthDetailId,
                requestType,
                authActivityId = req.AuthActivityId,
                ids,
                userId
            }, tx);

            // Insert rows that don't exist yet (within same scope)
            const string insertSql = @"
                insert into authworkgroup
                    (requesttype, authdetailid, authactivityid, workgroupworkbasketid, groupstatusid,
                     activeflag, createdon, createdby)
                select
                    @requestType,
                    @authDetailId,
                    case when @requestType = 'AUTH' then null else @authActivityId end,
                    v,
                    @groupStatusId,
                    true,
                    now(),
                    @userId
                from unnest(@ids) as v
                where not exists (
                    select 1
                    from authworkgroup awg
                    where awg.authdetailid = @authDetailId
                      and awg.requesttype = @requestType
                      and (
                            (@requestType = 'AUTH' and awg.authactivityid is null)
                         or (@requestType = 'ACTIVITY' and awg.authactivityid = @authActivityId)
                      )
                      and awg.workgroupworkbasketid = v
                );";

            await conn.ExecuteAsync(insertSql, new
            {
                requestType,
                authDetailId = req.AuthDetailId,
                authActivityId = req.AuthActivityId,
                ids,
                groupStatusId = req.GroupStatusId,
                userId
            }, tx);

            await tx.CommitAsync();
        }


        public async Task AcceptRejectAuthWorkgroupAsync(
            long authWorkgroupId,
            string actionType,          // "ACCEPT" or "REJECT"
            string? comment,
            int userId,
            int completedStatusId       // used only when ACCEPT
)
        {
            actionType = (actionType ?? "").Trim().ToUpperInvariant();
            if (actionType != "ACCEPT" && actionType != "REJECT")
                throw new ArgumentException("actionType must be ACCEPT or REJECT");

            await using var conn = CreateConn();
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted);

            // Lock target workgroup row
            const string getWgSql = @"
                select
                    authworkgroupid as AuthWorkgroupId,
                    authdetailid as AuthDetailId,
                    authactivityid as AuthActivityId,
                    requesttype as RequestType
                from authworkgroup
                where authworkgroupid = @authWorkgroupId
                  and activeflag = true
                for update;";

            var wg = await conn.QueryFirstOrDefaultAsync<AuthWorkgroupRow>(
                getWgSql, new { authWorkgroupId }, tx);

            if (wg == null || wg.AuthWorkgroupId <= 0)
                throw new InvalidOperationException("Workgroup assignment not found or inactive.");

            var requestType = (wg.RequestType ?? "").Trim().ToUpperInvariant();

            // Enforce your table rule: AUTH must have null activity, ACTIVITY must have non-null activity
            if (requestType == "AUTH" && wg.AuthActivityId != null)
                throw new InvalidOperationException("Invalid AUTH workgroup row: authactivityid must be NULL.");
            if (requestType == "ACTIVITY" && wg.AuthActivityId == null)
                throw new InvalidOperationException("Invalid ACTIVITY workgroup row: authactivityid is required.");

            // Prevent double-accept on the SAME authworkgroup row
            if (actionType == "ACCEPT")
            {
                const string alreadyAcceptedSql = @"
                    select 1
                    from authworkgroupaction
                    where authworkgroupid = @authWorkgroupId
                      and activeflag = true
                      and upper(actiontype) = 'ACCEPT'
                    limit 1;";

                var alreadyAccepted = await conn.ExecuteScalarAsync<int?>(
                    alreadyAcceptedSql, new { authWorkgroupId }, tx);

                if (alreadyAccepted.HasValue)
                    throw new InvalidOperationException("This workgroup assignment is already accepted.");
            }

            // Insert/upsert action for this user
            const string upsertActionSql = @"
                insert into authworkgroupaction
                    (authworkgroupid, userid, actiontype, actionon, comment, activeflag, createdon, createdby)
                values
                    (@authWorkgroupId, @userId, @actionType, now(), @comment, true, now(), @userId)
                on conflict (authworkgroupid, userid)
                do update set
                    actiontype = excluded.actiontype,
                    actionon   = now(),
                    comment    = excluded.comment,
                    updatedon  = now(),
                    updatedby  = @userId,
                    activeflag = true;";

            await conn.ExecuteAsync(
                upsertActionSql,
                new { authWorkgroupId, userId, actionType, comment },
                tx);

            // REJECT: nothing else to do
            if (actionType == "REJECT")
            {
                await tx.CommitAsync();
                return;
            }

            // ACCEPT: mark completed + assign auth (AUTH only) + deactivate other active rows in same scope
            const string completeWgSql = @"
                update authworkgroup
                set groupstatusid = @completedStatusId,
                    updatedon = now(),
                    updatedby = @userId
                where authworkgroupid = @authWorkgroupId;";

            await conn.ExecuteAsync(
                completeWgSql,
                new { authWorkgroupId, completedStatusId, userId },
                tx);

            // AUTH: assign auth to accepted user
            if (requestType == "AUTH")
            {
                const string assignAuthSql = @"
                    update authdetail
                    set authassignedto = @userId,
                        updatedon = now(),
                        updatedby = @userId
                    where authdetailid = @authDetailId
                      and deletedon is null;";

                await conn.ExecuteAsync(
                    assignAuthSql,
                    new { authDetailId = wg.AuthDetailId, userId },
                    tx);

                // Deactivate all other active AUTH workgroup rows for this authdetail
                const string deactivateOthersSql = @"
                    update authworkgroup
                    set activeflag = false,
                        updatedon = now(),
                        updatedby = @userId
                    where authdetailid = @authDetailId
                      and requesttype = 'AUTH'
                      and authactivityid is null
                      and activeflag = true
                      and authworkgroupid <> @authWorkgroupId;";

                await conn.ExecuteAsync(
                    deactivateOthersSql,
                    new { authDetailId = wg.AuthDetailId, authWorkgroupId, userId },
                    tx);
            }
            else if (requestType == "ACTIVITY")
            {
                // ACTIVITY: do NOT update authdetail.authassignedto (per your rule)
                // Deactivate other active ACTIVITY workgroup rows for this authdetail+authactivity
                const string deactivateOthersActivitySql = @"
                    update authworkgroup
                    set activeflag = false,
                        updatedon = now(),
                        updatedby = @userId
                    where authdetailid = @authDetailId
                      and requesttype = 'ACTIVITY'
                      and authactivityid = @authActivityId
                      and activeflag = true
                      and authworkgroupid <> @authWorkgroupId;";

                await conn.ExecuteAsync(
                    deactivateOthersActivitySql,
                    new { authDetailId = wg.AuthDetailId, authActivityId = wg.AuthActivityId, authWorkgroupId, userId },
                    tx);
            }
            else
            {
                throw new InvalidOperationException($"Unsupported requesttype '{wg.RequestType}'.");
            }

            await tx.CommitAsync();
        }

        private static int[] NormalizeWgWbIds(List<int>? ids, int? singleId)
        {
            if (ids != null && ids.Count > 0)
                return ids.Where(x => x > 0).Distinct().ToArray();

            return (singleId.HasValue && singleId.Value > 0)
                ? new[] { singleId.Value }
                : Array.Empty<int>();
        }

    }
}
