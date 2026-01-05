using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Data;
using System.Text.Json;


namespace CareNirvana.Service.Infrastructure.Repository
{
    internal static class GroupStatus
    {
        public const int Requested = 1;
        public const int Accepted = 2;
        public const int Rejected = 3;
    }

    public sealed class CaseActivityRepository : ICaseActivityRepository
    {
        private readonly string _connStr;

        public CaseActivityRepository(IConfiguration configuration)
        {
            _connStr = configuration.GetConnectionString("DefaultConnection");
        }

        private NpgsqlConnection Open() => new NpgsqlConnection(_connStr);

        public async Task<IReadOnlyList<CaseActivityRowDto>> GetByCaseAsync(
            int caseHeaderId, int memberDetailsId, int caseLevelId, string status, CancellationToken ct)
        {
            const string sql = @"
                    SELECT
                      ca.caseactivityid        AS CaseActivityId,
                      ca.caseheaderid          AS CaseHeaderId,
                      ca.memberdetailsid       AS MemberDetailsId,
                      ca.caselevelid           AS CaseLevelId,
                      ca.activitytypeid        AS ActivityTypeId,
                      ca.priorityid            AS PriorityId,
                      ca.followupdatetime      AS FollowUpDateTime,
                      ca.duedate               AS DueDate,
                      ca.referto               AS ReferTo,
                      ca.comment               AS Comment,
                      CASE
                        WHEN ca.referto IS NOT NULL THEN 'ACCEPTED'
                        WHEN EXISTS (
                          SELECT 1
                          FROM public.caseworkgroup cw
                          WHERE cw.requesttype = 'ACTIVITY'
                            AND cw.caseactivityid = ca.caseactivityid
                            AND cw.activeflag = true
                            AND cw.groupstatusid = @Requested
                        ) THEN 'REQUESTED'
                        WHEN EXISTS (
                          SELECT 1
                          FROM public.caseworkgroup cw
                          WHERE cw.requesttype = 'ACTIVITY'
                            AND cw.caseactivityid = ca.caseactivityid
                            AND cw.activeflag = true
                        )
                        AND NOT EXISTS (
                          SELECT 1
                          FROM public.caseworkgroup cw
                          WHERE cw.requesttype = 'ACTIVITY'
                            AND cw.caseactivityid = ca.caseactivityid
                            AND cw.activeflag = true
                            AND cw.groupstatusid <> @Rejected
                        ) THEN 'REJECTED'
                        ELSE 'OPEN'
                      END AS RequestStatus
                    FROM public.caseactivity ca
                    WHERE ca.activeflag = true
                      AND ca.caseheaderid = @CaseHeaderId
                      AND ca.memberdetailsid = @MemberDetailsId
                      AND ca.caselevelid = @CaseLevelId
                    ORDER BY ca.createdon DESC;
                    ";

            using var conn = Open();
            var rows = (await conn.QueryAsync<CaseActivityRowDto>(
                new CommandDefinition(sql, new
                {
                    CaseHeaderId = caseHeaderId,
                    MemberDetailsId = memberDetailsId,
                    CaseLevelId = caseLevelId,
                    Requested = GroupStatus.Requested,
                    Rejected = GroupStatus.Rejected
                }, cancellationToken: ct))).ToList();

            status = (status ?? "all").Trim().ToLowerInvariant();
            if (status == "all") return rows;

            return status switch
            {
                "requested" => rows.Where(x => x.RequestStatus == "REQUESTED").ToList(),
                "accepted" => rows.Where(x => x.RequestStatus == "ACCEPTED").ToList(),
                "rejected" => rows.Where(x => x.RequestStatus == "REJECTED").ToList(),
                "open" => rows.Where(x => x.RequestStatus == "OPEN").ToList(),
                _ => rows
            };
        }

        public async Task<CaseActivityRowDto?> GetByIdAsync(int caseActivityId, CancellationToken ct)
        {
            const string sql = @"
                SELECT
                  ca.caseactivityid AS CaseActivityId,
                  ca.caseheaderid   AS CaseHeaderId,
                  ca.memberdetailsid AS MemberDetailsId,
                  ca.caselevelid    AS CaseLevelId,
                  ca.activitytypeid AS ActivityTypeId,
                  ca.priorityid     AS PriorityId,
                  ca.followupdatetime AS FollowUpDateTime,
                  ca.duedate        AS DueDate,
                  ca.referto        AS ReferTo,
                  ca.comment        AS Comment,
                  CASE
                    WHEN ca.referto IS NOT NULL THEN 'ACCEPTED'
                    WHEN EXISTS (
                      SELECT 1 FROM public.caseworkgroup cw
                      WHERE cw.requesttype='ACTIVITY' AND cw.caseactivityid=ca.caseactivityid AND cw.activeflag=true AND cw.groupstatusid=@Requested
                    ) THEN 'REQUESTED'
                    WHEN EXISTS (
                      SELECT 1 FROM public.caseworkgroup cw
                      WHERE cw.requesttype='ACTIVITY' AND cw.caseactivityid=ca.caseactivityid AND cw.activeflag=true
                    )
                    AND NOT EXISTS (
                      SELECT 1 FROM public.caseworkgroup cw
                      WHERE cw.requesttype='ACTIVITY' AND cw.caseactivityid=ca.caseactivityid AND cw.activeflag=true AND cw.groupstatusid <> @Rejected
                    ) THEN 'REJECTED'
                    ELSE 'OPEN'
                  END AS RequestStatus
                FROM public.caseactivity ca
                WHERE ca.caseactivityid=@CaseActivityId AND ca.activeflag=true;
                ";
            using var conn = Open();
            return await conn.QueryFirstOrDefaultAsync<CaseActivityRowDto>(
                new CommandDefinition(sql, new
                {
                    CaseActivityId = caseActivityId,
                    Requested = GroupStatus.Requested,
                    Rejected = GroupStatus.Rejected
                }, cancellationToken: ct));
        }

        public async Task<int> InsertAsync(CaseActivityCreateDto dto, CancellationToken ct)
        {
            using var conn = Open();
            await conn.OpenAsync(ct);
            using var tx = conn.BeginTransaction();

            try
            {
                const string insertActivity = @"
                    INSERT INTO public.caseactivity
                    (
                      caseheaderid, memberdetailsid, caselevelid,
                      activitytypeid, priorityid, followupdatetime, duedate,
                      referto, comment, statusid,
                      activeflag, createdon, createdby
                    )
                    VALUES
                    (
                      @CaseHeaderId, @MemberDetailsId, @CaseLevelId,
                      @ActivityTypeId, @PriorityId, @FollowUpDateTime, @DueDate,
                      NULL, @Comment, @StatusId,
                      true, CURRENT_TIMESTAMP, @CreatedBy
                    )
                    RETURNING caseactivityid;
                    ";

                // For group request: referto must be NULL (enforced here)
                var newId = await conn.ExecuteScalarAsync<int>(
                    new CommandDefinition(insertActivity, new
                    {
                        dto.CaseHeaderId,
                        dto.MemberDetailsId,
                        dto.CaseLevelId,
                        dto.ActivityTypeId,
                        dto.PriorityId,
                        dto.FollowUpDateTime,
                        dto.DueDate,
                        dto.Comment,
                        dto.StatusId,
                        dto.CreatedBy
                    }, transaction: tx, cancellationToken: ct));

                if (dto.IsGroupRequest)
                {
                    var ids = dto.WorkGroupWorkBasketIds ?? new List<int>();
                    if (ids.Count == 0)
                        throw new InvalidOperationException("Group request requires WorkGroupWorkBasketIds.");

                    const string insertWorkgroup = @"
                        INSERT INTO public.caseworkgroup
                        (
                          requesttype, caseheaderid, caseactivityid, caselevelid,
                          workgroupworkbasketid, groupstatusid, activeflag, createdon, createdby
                        )
                        VALUES
                        (
                          'ACTIVITY', @CaseHeaderId, @CaseActivityId, @CaseLevelId,
                          @WorkGroupWorkBasketId, @Requested, true, CURRENT_TIMESTAMP, @CreatedBy
                        )
                        ON CONFLICT DO NOTHING;
                        ";
                    foreach (var wgwId in ids.Distinct())
                    {
                        await conn.ExecuteAsync(new CommandDefinition(insertWorkgroup, new
                        {
                            CaseHeaderId = dto.CaseHeaderId,
                            CaseActivityId = newId,
                            CaseLevelId = dto.CaseLevelId,
                            WorkGroupWorkBasketId = wgwId,
                            Requested = GroupStatus.Requested,
                            CreatedBy = dto.CreatedBy
                        }, transaction: tx, cancellationToken: ct));
                    }
                }

                await tx.CommitAsync(ct);
                return newId;
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }

        public async Task<bool> UpdateAsync(CaseActivityUpdateDto dto, CancellationToken ct)
        {
            const string sql = @"
                UPDATE public.caseactivity
                SET
                  activitytypeid = COALESCE(@ActivityTypeId, activitytypeid),
                  priorityid     = COALESCE(@PriorityId, priorityid),
                  followupdatetime = COALESCE(@FollowUpDateTime, followupdatetime),
                  duedate        = COALESCE(@DueDate, duedate),
                  comment        = COALESCE(@Comment, comment),
                  statusid       = COALESCE(@StatusId, statusid),
                  updatedon      = CURRENT_TIMESTAMP,
                  updatedby      = @UpdatedBy
                WHERE caseactivityid = @CaseActivityId
                  AND activeflag = true;
                ";
            using var conn = Open();
            var affected = await conn.ExecuteAsync(new CommandDefinition(sql, dto, cancellationToken: ct));
            return affected > 0;
        }

        public async Task<bool> DeleteAsync(int caseActivityId, int deletedBy, CancellationToken ct)
        {
            const string sql = @"
                UPDATE public.caseactivity
                SET activeflag = false,
                    deletedon = CURRENT_TIMESTAMP,
                    deletedby = @DeletedBy
                WHERE caseactivityid=@CaseActivityId AND activeflag=true;
                ";
            using var conn = Open();
            var affected = await conn.ExecuteAsync(new CommandDefinition(sql, new { CaseActivityId = caseActivityId, DeletedBy = deletedBy }, cancellationToken: ct));
            return affected > 0;
        }

        public async Task<bool> AcceptGroupActivityAsync(int caseActivityId, WorkgroupActionDto dto, CancellationToken ct)
        {
            using var conn = Open();
            await conn.OpenAsync(ct);
            using var tx = conn.BeginTransaction();

            try
            {
                // 1) Record action (UPSERT so user can change mind later if needed)
                const string upsertAction = @"
                    INSERT INTO public.caseworkgroupaction
                    (caseworkgroupid, userid, caselevelid, actiontype, actionon, comment, activeflag, createdon, createdby)
                    VALUES
                    (@CaseWorkgroupId, @UserId, @CaseLevelId, 'ACCEPT', CURRENT_TIMESTAMP, @Comment, true, CURRENT_TIMESTAMP, @UserId)
                    ON CONFLICT (caseworkgroupid, userid)
                    DO UPDATE SET
                      actiontype = 'ACCEPT',
                      actionon   = CURRENT_TIMESTAMP,
                      comment    = EXCLUDED.comment,
                      updatedon  = CURRENT_TIMESTAMP,
                      updatedby  = @UserId;
                    ";
                await conn.ExecuteAsync(new CommandDefinition(upsertAction, dto, transaction: tx, cancellationToken: ct));

                // 2) Mark this workgroup row accepted
                const string updateWg = @"
                    UPDATE public.caseworkgroup
                    SET groupstatusid = @Accepted,
                        updatedon = CURRENT_TIMESTAMP,
                        updatedby = @UserId
                    WHERE caseworkgroupid=@CaseWorkgroupId AND activeflag=true;
                    ";
                await conn.ExecuteAsync(new CommandDefinition(updateWg, new
                {
                    dto.CaseWorkgroupId,
                    dto.UserId,
                    Accepted = GroupStatus.Accepted
                }, transaction: tx, cancellationToken: ct));

                // 3) Update activity referto IF still null (first accept wins)
                const string updateActivity = @"
                    UPDATE public.caseactivity
                    SET referto = @UserId,
                        updatedon = CURRENT_TIMESTAMP,
                        updatedby = @UserId
                    WHERE caseactivityid = @CaseActivityId
                      AND activeflag = true
                      AND referto IS NULL;
                    ";
                var updated = await conn.ExecuteAsync(new CommandDefinition(updateActivity, new
                {
                    CaseActivityId = caseActivityId,
                    dto.UserId
                }, transaction: tx, cancellationToken: ct));

                // If already accepted by someone else, treat as false (conflict)
                if (updated == 0)
                {
                    await tx.RollbackAsync(ct);
                    return false;
                }

                await tx.CommitAsync(ct);
                return true;
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }

        public async Task<bool> RejectGroupActivityAsync(int caseActivityId, WorkgroupActionDto dto, CancellationToken ct)
        {
            using var conn = Open();
            await conn.OpenAsync(ct);
            using var tx = conn.BeginTransaction();

            try
            {
                // 1) UPSERT reject action
                const string upsertAction = @"
                    INSERT INTO public.caseworkgroupaction
                    (caseworkgroupid, userid, caselevelid, actiontype, actionon, comment, activeflag, createdon, createdby)
                    VALUES
                    (@CaseWorkgroupId, @UserId, @CaseLevelId, 'REJECT', CURRENT_TIMESTAMP, @Comment, true, CURRENT_TIMESTAMP, @UserId)
                    ON CONFLICT (caseworkgroupid, userid)
                    DO UPDATE SET
                      actiontype = 'REJECT',
                      actionon   = CURRENT_TIMESTAMP,
                      comment    = EXCLUDED.comment,
                      updatedon  = CURRENT_TIMESTAMP,
                      updatedby  = @UserId;
                    ";
                await conn.ExecuteAsync(new CommandDefinition(upsertAction, dto, transaction: tx, cancellationToken: ct));

                // 2) Check if ALL users of this workgroupworkbasket have rejected => mark groupstatus rejected
                const string getWgwId = @"
                    SELECT workgroupworkbasketid
                    FROM public.caseworkgroup
                    WHERE caseworkgroupid=@CaseWorkgroupId AND activeflag=true;
                    ";
                var wgwId = await conn.ExecuteScalarAsync<int>(
                    new CommandDefinition(getWgwId, new { dto.CaseWorkgroupId }, transaction: tx, cancellationToken: ct));

                // total active users in the workgroup for this basket
                const string totalUsersSql = @"
                    SELECT COUNT(DISTINCT uwg.userid)
                    FROM public.cfgworkgroupworkbasket wgw
                    JOIN public.cfguserworkgroup uwg
                      ON uwg.workgroupworkbasketid = wgw.workgroupid
                     AND uwg.activeflag = true
                    WHERE wgw.workgroupworkbasketid = @WorkGroupWorkBasketId;
                    ";
                var totalUsers = await conn.ExecuteScalarAsync<int>(
                    new CommandDefinition(totalUsersSql, new { WorkGroupWorkBasketId = wgwId }, transaction: tx, cancellationToken: ct));

                const string rejectedCountSql = @"
                    SELECT COUNT(*)
                    FROM public.caseworkgroupaction
                    WHERE caseworkgroupid = @CaseWorkgroupId
                      AND activeflag = true
                      AND actiontype = 'REJECT';
                    ";
                var rejectedCount = await conn.ExecuteScalarAsync<int>(
                    new CommandDefinition(rejectedCountSql, new { dto.CaseWorkgroupId }, transaction: tx, cancellationToken: ct));

                if (totalUsers > 0 && rejectedCount >= totalUsers)
                {
                    const string markRejected = @"
                        UPDATE public.caseworkgroup
                        SET groupstatusid = @Rejected,
                            updatedon = CURRENT_TIMESTAMP,
                            updatedby = @UserId
                        WHERE caseworkgroupid=@CaseWorkgroupId AND activeflag=true;
                        ";
                    await conn.ExecuteAsync(new CommandDefinition(markRejected, new
                    {
                        dto.CaseWorkgroupId,
                        dto.UserId,
                        Rejected = GroupStatus.Rejected
                    }, transaction: tx, cancellationToken: ct));
                }

                await tx.CommitAsync(ct);
                return true;
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }

        public async Task<IReadOnlyList<CaseActivityRowDto>> GetPendingRequestsForUserAsync(
            int userId, int caseHeaderId, int memberDetailsId, int caseLevelId, CancellationToken ct)
        {
            // Pending for this user = group requested AND user is in the workgroup AND user has not acted yet AND referto is still null
            const string sql = @"
                SELECT DISTINCT
                  ca.caseactivityid AS CaseActivityId,
                  ca.caseheaderid   AS CaseHeaderId,
                  ca.memberdetailsid AS MemberDetailsId,
                  ca.caselevelid    AS CaseLevelId,
                  ca.activitytypeid AS ActivityTypeId,
                  ca.priorityid     AS PriorityId,
                  ca.followupdatetime AS FollowUpDateTime,
                  ca.duedate        AS DueDate,
                  ca.referto        AS ReferTo,
                  ca.comment        AS Comment,
                  'REQUESTED'       AS RequestStatus
                FROM public.caseactivity ca
                JOIN public.caseworkgroup cw
                  ON cw.requesttype='ACTIVITY'
                 AND cw.caseactivityid = ca.caseactivityid
                 AND cw.activeflag = true
                 AND cw.groupstatusid = @Requested
                JOIN public.cfgworkgroupworkbasket wgw
                  ON wgw.workgroupworkbasketid = cw.workgroupworkbasketid
                JOIN public.cfguserworkgroup uwg
                  ON uwg.workgroupworkbasketid = wgw.workgroupid
                 AND uwg.userid = @UserId
                 AND uwg.activeflag = true
                LEFT JOIN public.caseworkgroupaction cwa
                  ON cwa.caseworkgroupid = cw.caseworkgroupid
                 AND cwa.userid = @UserId
                 AND cwa.activeflag = true
                WHERE ca.activeflag = true
                  AND ca.caseheaderid = @CaseHeaderId
                  AND ca.memberdetailsid = @MemberDetailsId
                  AND ca.caselevelid = @CaseLevelId
                  AND ca.referto IS NULL
                  AND cwa.caseworkgroupactionid IS NULL
                ;
                ";
            using var conn = Open();
            var rows = await conn.QueryAsync<CaseActivityRowDto>(new CommandDefinition(sql, new
            {
                UserId = userId,
                CaseHeaderId = caseHeaderId,
                MemberDetailsId = memberDetailsId,
                CaseLevelId = caseLevelId,
                Requested = GroupStatus.Requested
            }, cancellationToken: ct));

            return rows.ToList();
        }

        public async Task<IReadOnlyList<CaseActivityRowDto>> GetAcceptedForUserAsync(
            int userId, int caseHeaderId, int memberDetailsId, int caseLevelId, CancellationToken ct)
        {
            // Accepted for user = activity referto=user OR user accepted action record
            const string sql = @"
                SELECT DISTINCT
                  ca.caseactivityid AS CaseActivityId,
                  ca.caseheaderid   AS CaseHeaderId,
                  ca.memberdetailsid AS MemberDetailsId,
                  ca.caselevelid    AS CaseLevelId,
                  ca.activitytypeid AS ActivityTypeId,
                  ca.priorityid     AS PriorityId,
                  ca.followupdatetime AS FollowUpDateTime,
                  ca.duedate        AS DueDate,
                  ca.referto        AS ReferTo,
                  ca.comment        AS Comment,
                  'ACCEPTED'        AS RequestStatus
                FROM public.caseactivity ca
                LEFT JOIN public.caseworkgroup cw
                  ON cw.requesttype='ACTIVITY'
                 AND cw.caseactivityid = ca.caseactivityid
                 AND cw.activeflag = true
                LEFT JOIN public.caseworkgroupaction cwa
                  ON cwa.caseworkgroupid = cw.caseworkgroupid
                 AND cwa.userid = @UserId
                 AND cwa.activeflag = true
                WHERE ca.activeflag=true
                  AND ca.caseheaderid=@CaseHeaderId
                  AND ca.memberdetailsid=@MemberDetailsId
                  AND ca.caselevelid=@CaseLevelId
                  AND (
                        ca.referto = @UserId
                        OR (cwa.actiontype='ACCEPT')
                      )
                ;
                ";
            using var conn = Open();
            var rows = await conn.QueryAsync<CaseActivityRowDto>(new CommandDefinition(sql, new
            {
                UserId = userId,
                CaseHeaderId = caseHeaderId,
                MemberDetailsId = memberDetailsId,
                CaseLevelId = caseLevelId
            }, cancellationToken: ct));

            return rows.ToList();
        } // 

        public async Task<CaseActivityTemplateResponse?> GetCaseActivityTemplateAsync(int caseTemplateId, CancellationToken ct = default)
        {
            const string sql = @"
                SELECT jsonb_path_query_first(
                         ct.jsoncontent::jsonb,
                         '$.** ? (@.sectionName == ""Case Activity"")'
                       ) AS section
                FROM cfgcasetemplate ct
                WHERE ct.casetemplateid = @caseTemplateId;";

            await using var conn = Open();

            var sectionJson = await conn.ExecuteScalarAsync<string>(
                new CommandDefinition(sql, new { caseTemplateId }, cancellationToken: ct)
            );

            if (string.IsNullOrWhiteSpace(sectionJson))
                return null;

            // Parse to JsonElement so you can send it straight to UI or map it later
            using var doc = JsonDocument.Parse(sectionJson);

            return new CaseActivityTemplateResponse
            {
                CaseTemplateId = caseTemplateId,
                SectionName = "Case Activity",
                Section = doc.RootElement.Clone()
            };
        }
    }

}
