using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Collections.Generic;
using System.Data;
using System.Text;


namespace CareNirvana.Service.Infrastructure.Repository
{

    public class MemberActivityRepository : IMemberActivity
    {
        private readonly string _connectionString;

        public MemberActivityRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        private NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }

        #region Insert

        /// <summary>
        /// Creates a member activity, optionally linking it to a work group work basket.
        /// If workGroupWorkBasketId is provided, isWorkBasket will usually be true.
        /// Returns the new MemberActivityId.
        /// </summary>
        public async Task<int> CreateMemberActivityAsync(
            MemberActivity activity,
            int? workGroupWorkBasketId,
            int createdBy,
            CancellationToken cancellationToken = default)
        {
            const string insertActivitySql = @"
                INSERT INTO public.memberactivity
                (
                    activitytypeid,
                    priorityid,
                    memberdetailsid,
                    followupdatetime,
                    duedate,
                    referto,
                    isworkbasket,
                    queueid,
                    comment,
                    statusid,
                    performeddatetime,
                    performedby,
                    activeflag,
                    createdon,
                    createdby
                )
                VALUES
                (
                    @ActivityTypeId,
                    @PriorityId,
                    @MemberDetailsId,
                    @FollowUpDateTime,
                    @DueDate,
                    @ReferTo,
                    COALESCE(@IsWorkBasket, @IsWorkBasketDefault),
                    @QueueId,
                    @Comment,
                    @StatusId,
                    @PerformedDateTime,
                    @PerformedBy,
                    COALESCE(@ActiveFlag, true),
                    CURRENT_TIMESTAMP,
                    @CreatedBy
                )
                RETURNING memberactivityid;";

            const string insertWorkGroupSql = @"
                INSERT INTO public.memberactivityworkgroup
                (
                    memberactivityid,
                    workgroupworkbasketid,
                    groupstatusid,
                    activeflag,
                    createdon,
                    createdby
                )
                VALUES
                (
                    @MemberActivityId,
                    @WorkGroupWorkBasketId,
                    @GroupStatusId,
                    true,
                    CURRENT_TIMESTAMP,
                    @CreatedBy
                );";

            await using var conn = GetConnection();
            await conn.OpenAsync(cancellationToken);

            await using var tx = await conn.BeginTransactionAsync(cancellationToken);

            try
            {
                var newId = await conn.ExecuteScalarAsync<int>(
                    new CommandDefinition(
                        insertActivitySql,
                        new
                        {
                            activity.ActivityTypeId,
                            activity.PriorityId,
                            activity.MemberDetailsId,
                            activity.FollowUpDateTime,
                            activity.DueDate,
                            activity.ReferTo,
                            activity.IsWorkBasket,
                            IsWorkBasketDefault = workGroupWorkBasketId.HasValue, // true if group activity
                            activity.QueueId,
                            activity.Comment,
                            activity.StatusId,
                            activity.PerformedDateTime,
                            activity.PerformedBy,
                            activity.ActiveFlag,
                            CreatedBy = createdBy
                        },
                        tx,
                        cancellationToken: cancellationToken
                    ));

                if (workGroupWorkBasketId.HasValue)
                {
                    // Optionally set a default GroupStatusId (e.g. "Pending") from config if you want
                    await conn.ExecuteAsync(
                        new CommandDefinition(
                            insertWorkGroupSql,
                            new
                            {
                                MemberActivityId = newId,
                                WorkGroupWorkBasketId = workGroupWorkBasketId.Value,
                                GroupStatusId = (int?)null,
                                CreatedBy = createdBy
                            },
                            tx,
                            cancellationToken: cancellationToken
                        ));
                }

                await tx.CommitAsync(cancellationToken);
                return newId;
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                throw;
            }
        }

        #endregion

        #region Update Activity

        /// <summary>
        /// Updates core fields of the member activity (dates, priority, comment, status, etc.).
        /// Does NOT change work group assignment or accept/reject info.
        /// </summary>
        public async Task<int> UpdateMemberActivityAsync(
            MemberActivity activity,
            int updatedBy,
            CancellationToken cancellationToken = default)
        {
            const string updateSql = @"
                UPDATE public.memberactivity
                SET
                    activitytypeid = @ActivityTypeId,
                    priorityid = @PriorityId,
                    memberdetailsid = @MemberDetailsId,
                    followupdatetime = @FollowUpDateTime,
                    duedate = @DueDate,
                    queueid = @QueueId,
                    comment = @Comment,
                    statusid = @StatusId,
                    performeddatetime = @PerformedDateTime,
                    performedby = @PerformedBy,
                    activeflag = COALESCE(@ActiveFlag, activeflag),
                    updatedon = CURRENT_TIMESTAMP,
                    updatedby = @UpdatedBy
                WHERE memberactivityid = @MemberActivityId
                  AND deletedon IS NULL;";

            await using var conn = GetConnection();
            return await conn.ExecuteAsync(
                new CommandDefinition(
                    updateSql,
                    new
                    {
                        activity.ActivityTypeId,
                        activity.PriorityId,
                        activity.MemberDetailsId,
                        activity.FollowUpDateTime,
                        activity.DueDate,
                        activity.QueueId,
                        activity.Comment,
                        activity.StatusId,
                        activity.PerformedDateTime,
                        activity.PerformedBy,
                        activity.ActiveFlag,
                        UpdatedBy = updatedBy,
                        activity.MemberActivityId
                    },
                    cancellationToken: cancellationToken
                ));
        }

        #endregion

        #region Work Group Accept / Reject

        /// <summary>
        /// Records a reject action for a user on a work group activity.
        /// One row per user per work group activity, so we use upsert.
        /// </summary>
        public async Task<int> RejectWorkGroupActivityAsync(
            int memberActivityWorkGroupId,
            int userId,
            string comment,
            CancellationToken cancellationToken = default)
        {
            const string sql = @"
                INSERT INTO public.memberactivityworkgroupaction
                (
                    memberactivityworkgroupid,
                    userid,
                    actiontype,
                    actionon,
                    comment,
                    activeflag,
                    createdon,
                    createdby
                )
                VALUES
                (
                    @MemberActivityWorkGroupId,
                    @UserId,
                    'Rejected',
                    CURRENT_TIMESTAMP,
                    @Comment,
                    true,
                    CURRENT_TIMESTAMP,
                    @UserId
                )
                ON CONFLICT (memberactivityworkgroupid, userid)
                DO UPDATE SET
                    actiontype = 'Rejected',
                    actionon = CURRENT_TIMESTAMP,
                    comment = @Comment,
                    activeflag = true,
                    updatedon = CURRENT_TIMESTAMP,
                    updatedby = @UserId;";

            await using var conn = GetConnection();
            return await conn.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        MemberActivityWorkGroupId = memberActivityWorkGroupId,
                        UserId = userId,
                        Comment = comment
                    },
                    cancellationToken: cancellationToken
                ));
        }

        /// <summary>
        /// Accepts (claims) a work group activity for a particular user.
        /// Steps:
        /// 1. Insert/Update action as 'Accepted'
        /// 2. Update memberactivity.referto and isworkbasket (only if it is not already assigned)
        /// 3. Optionally deactivate the work group record (so it no longer appears in pool)
        /// Returns affected rows of activity update (0 means someone else already claimed it).
        /// </summary>
        public async Task<int> AcceptWorkGroupActivityAsync(
            int memberActivityWorkGroupId,
            int userId,
            string comment,
            CancellationToken cancellationToken = default)
        {
            const string insertActionSql = @"
                INSERT INTO public.memberactivityworkgroupaction
                (
                    memberactivityworkgroupid,
                    userid,
                    actiontype,
                    actionon,
                    comment,
                    activeflag,
                    createdon,
                    createdby
                )
                VALUES
                (
                    @MemberActivityWorkGroupId,
                    @UserId,
                    'Accepted',
                    CURRENT_TIMESTAMP,
                    @Comment,
                    true,
                    CURRENT_TIMESTAMP,
                    @UserId
                )
                ON CONFLICT (memberactivityworkgroupid, userid)
                DO UPDATE SET
                    actiontype = 'Accepted',
                    actionon = CURRENT_TIMESTAMP,
                    comment = @Comment,
                    activeflag = true,
                    updatedon = CURRENT_TIMESTAMP,
                    updatedby = @UserId;";

            // Only allow the first accept to set referto
            const string updateActivitySql = @"
                UPDATE public.memberactivity
                SET
                    referto = @UserId,
                    isworkbasket = false,
                    updatedon = CURRENT_TIMESTAMP,
                    updatedby = @UserId
                WHERE memberactivityid = (
                    SELECT memberactivityid
                    FROM public.memberactivityworkgroup
                    WHERE memberactivityworkgroupid = @MemberActivityWorkGroupId
                )
                AND referto IS NULL
                AND deletedon IS NULL;";

            // Optional: close the work group pool entry
            const string updateWorkGroupSql = @"
                UPDATE public.memberactivityworkgroup
                SET
                    activeflag = false,
                    updatedon = CURRENT_TIMESTAMP,
                    updatedby = @UserId
                WHERE memberactivityworkgroupid = @MemberActivityWorkGroupId;";

            await using var conn = GetConnection();
            await conn.OpenAsync(cancellationToken);

            await using var tx = await conn.BeginTransactionAsync(cancellationToken);

            try
            {
                // 1) record action
                await conn.ExecuteAsync(
                    new CommandDefinition(
                        insertActionSql,
                        new
                        {
                            MemberActivityWorkGroupId = memberActivityWorkGroupId,
                            UserId = userId,
                            Comment = comment
                        },
                        tx,
                        cancellationToken: cancellationToken
                    ));

                // 2) claim activity (only first wins)
                var affectedActivity = await conn.ExecuteAsync(
                    new CommandDefinition(
                        updateActivitySql,
                        new
                        {
                            MemberActivityWorkGroupId = memberActivityWorkGroupId,
                            UserId = userId
                        },
                        tx,
                        cancellationToken: cancellationToken
                    ));

                if (affectedActivity == 0)
                {
                    // someone else already claimed; roll back action insert as well
                    await tx.RollbackAsync(cancellationToken);
                    return 0;
                }

                // 3) close pool row
                await conn.ExecuteAsync(
                    new CommandDefinition(
                        updateWorkGroupSql,
                        new
                        {
                            MemberActivityWorkGroupId = memberActivityWorkGroupId,
                            UserId = userId
                        },
                        tx,
                        cancellationToken: cancellationToken
                    ));

                await tx.CommitAsync(cancellationToken);
                return affectedActivity;
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                throw;
            }
        }

        /// <summary>
        /// Optional: update group status id (e.g. Pending / Claimed / Closed) if you use it.
        /// </summary>
        public async Task<int> UpdateWorkGroupStatusAsync(
            int memberActivityWorkGroupId,
            int? groupStatusId,
            int updatedBy,
            CancellationToken cancellationToken = default)
        {
            const string sql = @"
                UPDATE public.memberactivityworkgroup
                SET
                    groupstatusid = @GroupStatusId,
                    updatedon = CURRENT_TIMESTAMP,
                    updatedby = @UpdatedBy
                WHERE memberactivityworkgroupid = @MemberActivityWorkGroupId;";

            await using var conn = GetConnection();
            return await conn.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        MemberActivityWorkGroupId = memberActivityWorkGroupId,
                        GroupStatusId = groupStatusId,
                        UpdatedBy = updatedBy
                    },
                    cancellationToken: cancellationToken
                ));
        }

        #endregion

        #region Delete (Soft Delete)

        /// <summary>
        /// Soft deletes an activity and deactivates its work group records.
        /// </summary>
        public async Task<int> DeleteMemberActivityAsync(
            int memberActivityId,
            int deletedBy,
            CancellationToken cancellationToken = default)
        {
            const string deleteActivitySql = @"
                UPDATE public.memberactivity
                SET
                    activeflag = false,
                    deletedon = CURRENT_TIMESTAMP,
                    deletedby = @DeletedBy,
                    updatedon = CURRENT_TIMESTAMP,
                    updatedby = @DeletedBy
                WHERE memberactivityid = @MemberActivityId
                  AND deletedon IS NULL;";

            const string deactivateWorkGroupSql = @"
                UPDATE public.memberactivityworkgroup
                SET
                    activeflag = false,
                    updatedon = CURRENT_TIMESTAMP,
                    updatedby = @DeletedBy
                WHERE memberactivityid = @MemberActivityId;";

            await using var conn = GetConnection();
            await conn.OpenAsync(cancellationToken);

            await using var tx = await conn.BeginTransactionAsync(cancellationToken);

            try
            {
                var affectedActivity = await conn.ExecuteAsync(
                    new CommandDefinition(
                        deleteActivitySql,
                        new
                        {
                            MemberActivityId = memberActivityId,
                            DeletedBy = deletedBy
                        },
                        tx,
                        cancellationToken: cancellationToken
                    ));

                await conn.ExecuteAsync(
                    new CommandDefinition(
                        deactivateWorkGroupSql,
                        new
                        {
                            MemberActivityId = memberActivityId,
                            DeletedBy = deletedBy
                        },
                        tx,
                        cancellationToken: cancellationToken
                    ));

                await tx.CommitAsync(cancellationToken);
                return affectedActivity;
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task<IEnumerable<MemberActivityRequestItem>> GetRequestActivitiesAsync(
    IEnumerable<int> workGroupWorkBasketIds,
    DateTime? fromFollowUpDate,
    DateTime? toFollowUpDate,
    int? memberDetailsId,
    CancellationToken cancellationToken)
        {
            var idsArray = (workGroupWorkBasketIds ?? Array.Empty<int>()).ToArray();
            if (idsArray.Length == 0)
            {
                // no baskets -> no data
                return Array.Empty<MemberActivityRequestItem>();
            }

            var sql = new StringBuilder(@"
                SELECT
                    ma.memberactivityid AS MemberActivityId,
                    maw.memberactivityworkgroupid AS MemberActivityWorkGroupId,
                    maw.workgroupworkbasketid AS WorkGroupWorkBasketId,
                    ma.memberdetailsid AS MemberDetailsId,
                    ma.activitytypeid AS ActivityTypeId,
                    ma.priorityid AS PriorityId,
                    ma.followupdatetime AS FollowUpDateTime,
                    ma.duedate AS DueDate,
                    ma.comment AS Comment,
                    ma.statusid AS StatusId,
                    COALESCE(rej.rejectedcount, 0) AS RejectedCount,
                    COALESCE(rej.rejecteduserids, ARRAY[]::integer[]) AS RejectedUserIds
                FROM public.memberactivity ma
                JOIN public.memberactivityworkgroup maw
                    ON maw.memberactivityid = ma.memberactivityid
                LEFT JOIN (
                    SELECT
                        memberactivityworkgroupid,
                        COUNT(*) FILTER (WHERE actiontype = 'Rejected') AS rejectedcount,
                        ARRAY_AGG(userid) FILTER (WHERE actiontype = 'Rejected') AS rejecteduserids
                    FROM public.memberactivityworkgroupaction
                    WHERE activeflag = true
                    GROUP BY memberactivityworkgroupid
                ) rej
                    ON rej.memberactivityworkgroupid = maw.memberactivityworkgroupid
                WHERE ma.deletedon IS NULL
                  AND (ma.activeflag IS DISTINCT FROM false)
                  AND ma.referto IS NULL
                  AND ma.isworkbasket = true
                  AND maw.activeflag = true
                  AND maw.workgroupworkbasketid = ANY(@WorkGroupWorkBasketIds)
                ");

            var parameters = new DynamicParameters();
            parameters.Add("WorkGroupWorkBasketIds", idsArray);

            if (fromFollowUpDate.HasValue)
            {
                sql.Append("  AND ma.followupdatetime >= @FromFollowUpDate");
                parameters.Add("FromFollowUpDate", fromFollowUpDate);
            }

            if (toFollowUpDate.HasValue)
            {
                sql.Append("  AND ma.followupdatetime <= @ToFollowUpDate");
                parameters.Add("ToFollowUpDate", toFollowUpDate);
            }

            if (memberDetailsId.HasValue)
            {
                sql.Append("  AND ma.memberdetailsid = @MemberDetailsId");
                parameters.Add("MemberDetailsId", memberDetailsId);
            }

            sql.Append(@" ORDER BY ma.followupdatetime NULLS LAST, ma.createdon DESC;");

            await using var conn = GetConnection();
            return await conn.QueryAsync<MemberActivityRequestItem>(
                new CommandDefinition(
                    sql.ToString(),
                    parameters,
                    cancellationToken: cancellationToken
                ));
        }

        public async Task<IEnumerable<MemberActivityCurrentItem>> GetCurrentActivitiesAsync(
    IEnumerable<int> userIds,
    DateTime? fromFollowUpDate,
    DateTime? toFollowUpDate,
    int? memberDetailsId,
    CancellationToken cancellationToken)
        {
            var userIdArray = (userIds ?? Array.Empty<int>()).ToArray();
            if (userIdArray.Length == 0)
            {
                // no users -> no data
                return Array.Empty<MemberActivityCurrentItem>();
            }

            var sql = new StringBuilder(@"
                SELECT
                    ma.memberactivityid AS MemberActivityId,
                    ma.memberdetailsid AS MemberDetailsId,
                    ma.activitytypeid AS ActivityTypeId,
                    ma.priorityid AS PriorityId,
                    ma.followupdatetime AS FollowUpDateTime,
                    ma.duedate AS DueDate,
                    ma.comment AS Comment,
                    ma.statusid AS StatusId,
                    ma.referto AS ReferTo,
                    ma.performeddatetime AS PerformedDateTime,
                    ma.performedby AS PerformedBy
                FROM public.memberactivity ma
                WHERE ma.deletedon IS NULL
                  AND (ma.activeflag IS DISTINCT FROM false)
                  AND ma.referto = ANY(@UserIds)
                ");

            var parameters = new DynamicParameters();
            parameters.Add("UserIds", userIdArray);

            if (fromFollowUpDate.HasValue)
            {
                sql.Append("  AND ma.followupdatetime >= @FromFollowUpDate");
                parameters.Add("FromFollowUpDate", fromFollowUpDate);
            }

            if (toFollowUpDate.HasValue)
            {
                sql.Append("  AND ma.followupdatetime <= @ToFollowUpDate");
                parameters.Add("ToFollowUpDate", toFollowUpDate);
            }

            if (memberDetailsId.HasValue)
            {
                sql.Append("  AND ma.memberdetailsid = @MemberDetailsId");
                parameters.Add("MemberDetailsId", memberDetailsId);
            }

            sql.Append(@" ORDER BY ma.followupdatetime NULLS LAST, ma.createdon DESC;");

            await using var conn = GetConnection();
            return await conn.QueryAsync<MemberActivityCurrentItem>(
                new CommandDefinition(
                    sql.ToString(),
                    parameters,
                    cancellationToken: cancellationToken
                ));
        }

        public async Task<MemberActivityDetailItem?> GetMemberActivityDetailAsync(int memberActivityId, CancellationToken cancellationToken = default)
        {
            const string sql = @"
                SELECT
            -- activity
                ma.memberactivityid          AS MemberActivityId,
                ma.memberdetailsid           AS MemberDetailsId,
                ma.activitytypeid            AS ActivityTypeId,
                ma.priorityid                AS PriorityId,
                ma.followupdatetime          AS FollowUpDateTime,
                ma.duedate                   AS DueDate,
                ma.comment                   AS Comment,
                ma.statusid                  AS StatusId,
                ma.referto                   AS ReferTo,
                COALESCE(ma.isworkbasket, false) AS IsWorkBasket,

                -- workbasket info (if any)
                maw.memberactivityworkgroupid AS MemberActivityWorkGroupId,
                maw.workgroupworkbasketid     AS WorkGroupWorkBasketId,

                -- assigned users from cfguserworkgroup
                cuwg.userid                  AS UserId,

                -- join to your user table if you want names; adjust table/columns as needed
                u.username                   AS UserFullName,

                -- derived status per user
                CASE
                    WHEN mwa.actiontype = 'Rejected' THEN 'Rejected'
                    WHEN mwa.actiontype = 'Accepted' THEN 'Accepted'
                    ELSE 'Request'
                END                          AS Status
                FROM public.memberactivity ma
                LEFT JOIN public.memberactivityworkgroup maw
                    ON maw.memberactivityid = ma.memberactivityid
                  --AND (maw.activeflag IS DISTINCT FROM false)

                -- all assigned users for this workgroup-workbasket
                LEFT JOIN public.cfguserworkgroup cuwg
                    ON cuwg.workgroupworkbasketid = maw.workgroupworkbasketid
                   AND (cuwg.activeflag IS DISTINCT FROM false)

                -- any action those users took on this workgroup activity
                LEFT JOIN public.memberactivityworkgroupaction mwa
                    ON mwa.memberactivityworkgroupid = maw.memberactivityworkgroupid
                   AND mwa.userid = cuwg.userid
                   AND (mwa.activeflag IS DISTINCT FROM false)

                -- user table – change to your actual name (e.g., appuser, sysuser)
                LEFT JOIN public.securityuser u
                    ON u.userid = cuwg.userid

                WHERE ma.memberactivityid = @MemberActivityId
                  AND ma.deletedon IS NULL; ";

            await using var conn = GetConnection();

            var lookup = new Dictionary<int, MemberActivityDetailItem>();

            var result = await conn.QueryAsync<MemberActivityDetailItem, MemberActivityAssignedUserItem, MemberActivityDetailItem>(
                new CommandDefinition(
                    sql,
                    new { MemberActivityId = memberActivityId },
                    cancellationToken: cancellationToken
                ),
                (activity, user) =>
                {
                    if (!lookup.TryGetValue(activity.MemberActivityId, out var agg))
                    {
                        agg = activity;
                        agg.AssignedUsers = new List<MemberActivityAssignedUserItem>();
                        lookup.Add(agg.MemberActivityId, agg);
                    }

                    // For non-workbasket activities maw will be null, so no users
                    if (user != null && user.UserId != 0)
                    {
                        // Avoid duplicates just in case
                        if (!agg.AssignedUsers.Any(x => x.UserId == user.UserId))
                        {
                            agg.AssignedUsers.Add(user);
                        }
                    }

                    return agg;
                },
                splitOn: "UserId"
            );

            return lookup.Values.FirstOrDefault();
        }

        #endregion
    }

}
