using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareNirvana.Service.Infrastructure.Repository
{
    using CareNirvana.Service.Domain.Model;
    using Microsoft.Extensions.Configuration;
    using Npgsql;
    using NpgsqlTypes;
    using System.Data;

    public class AuthActivityRepository : IAuthActivityRepository
    {
        private readonly string _connectionString;

        public AuthActivityRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        private NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }

        public async Task<IEnumerable<AuthActivity>> GetAllAsync(int authdetailid)
        {
            var result = new List<AuthActivity>();

            using var conn = GetConnection();
            await conn.OpenAsync();

            var cmd = new NpgsqlCommand("SELECT * FROM authactivity WHERE authdetailid = @id and deletedon IS NULL", conn);
            cmd.Parameters.AddWithValue("id", authdetailid);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                result.Add(new AuthActivity
                {
                    AuthActivityId = reader.GetInt32(reader.GetOrdinal("authactivityid")),
                    AuthDetailId = reader["authdetailid"] as int?,
                    ActivityTypeId = reader["activitytypeid"] as int?,
                    PriorityId = reader["priorityid"] as int?,
                    ProviderId = reader["providerid"] as int?,
                    FollowUpDateTime = reader["followupdatetime"] as DateTime?,
                    DueDate = reader["duedate"] as DateTime?,
                    ReferredTo = reader["referredto"] as int?,
                    IsWorkBasket = reader["isworkbasket"] as bool?,
                    QueueId = reader["queueid"] as int?,
                    Comment = reader["comment"] as string,
                    StatusId = reader["statusid"] as int?,
                    PerformedDateTime = reader["performeddatetime"] as DateTime?,
                    PerformedBy = reader["performedby"] as int?,
                    ActiveFlag = reader["activeflag"] as bool?,
                    CreatedOn = reader.GetDateTime(reader.GetOrdinal("createdon")),
                    CreatedBy = reader["createdby"] as int?,
                    UpdatedOn = reader["updatedon"] as DateTime?,
                    UpdatedBy = reader["updatedby"] as int?,
                    DeletedOn = reader["deletedon"] as DateTime?,
                    DeletedBy = reader["deletedby"] as int?
                });
            }

            return result;
        }

        public async Task<AuthActivity?> GetByIdAsync(int id)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            var cmd = new NpgsqlCommand("SELECT * FROM authactivity WHERE authactivityid = @id", conn);
            cmd.Parameters.AddWithValue("id", id);

            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new AuthActivity
                {
                    AuthActivityId = reader.GetInt32(reader.GetOrdinal("authactivityid")),
                    AuthDetailId = reader["authdetailid"] as int?,
                    ActivityTypeId = reader["activitytypeid"] as int?,
                    PriorityId = reader["priorityid"] as int?,
                    ProviderId = reader["providerid"] as int?,
                    FollowUpDateTime = reader["followupdatetime"] as DateTime?,
                    DueDate = reader["duedate"] as DateTime?,
                    ReferredTo = reader["referredto"] as int?,
                    IsWorkBasket = reader["isworkbasket"] as bool?,
                    QueueId = reader["queueid"] as int?,
                    Comment = reader["comment"] as string,
                    StatusId = reader["statusid"] as int?,
                    PerformedDateTime = reader["performeddatetime"] as DateTime?,
                    PerformedBy = reader["performedby"] as int?,
                    ActiveFlag = reader["activeflag"] as bool?,
                    CreatedOn = reader.GetDateTime(reader.GetOrdinal("createdon")),
                    CreatedBy = reader["createdby"] as int?,
                    UpdatedOn = reader["updatedon"] as DateTime?,
                    UpdatedBy = reader["updatedby"] as int?,
                    DeletedOn = reader["deletedon"] as DateTime?,
                    DeletedBy = reader["deletedby"] as int?
                };
            }

            return null;
        }

        public async Task<AuthActivity> InsertAsync(AuthActivity activity)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            var cmd = new NpgsqlCommand(@"
            INSERT INTO authactivity 
            (authdetailid, activitytypeid, priorityid, providerid, followupdatetime, duedate, referredto, isworkbasket, queueid, comment, statusid, performeddatetime, performedby, activeflag, createdon, createdby) 
            VALUES 
            (@authdetailid, @activitytypeid, @priorityid, @providerid, @followupdatetime, @duedate, @referredto, @isworkbasket, @queueid, @comment, @statusid, @performeddatetime, @performedby, @activeflag, @createdon, @createdby)
            RETURNING authactivityid", conn);

            cmd.Parameters.AddWithValue("authdetailid", (object?)activity.AuthDetailId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("activitytypeid", (object?)activity.ActivityTypeId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("priorityid", (object?)activity.PriorityId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("providerid", (object?)activity.ProviderId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("followupdatetime", (object?)activity.FollowUpDateTime ?? DBNull.Value);
            cmd.Parameters.AddWithValue("duedate", (object?)activity.DueDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("referredto", (object?)activity.ReferredTo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("isworkbasket", (object?)activity.IsWorkBasket ?? DBNull.Value);
            cmd.Parameters.AddWithValue("queueid", (object?)activity.QueueId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("comment", (object?)activity.Comment ?? DBNull.Value);
            cmd.Parameters.AddWithValue("statusid", (object?)activity.StatusId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("performeddatetime", (object?)activity.PerformedDateTime ?? DBNull.Value);
            cmd.Parameters.AddWithValue("performedby", (object?)activity.PerformedBy ?? DBNull.Value);
            cmd.Parameters.AddWithValue("activeflag", (object?)activity.ActiveFlag ?? DBNull.Value);
            cmd.Parameters.AddWithValue("createdon", activity.CreatedOn);
            cmd.Parameters.AddWithValue("createdby", (object?)activity.CreatedBy ?? DBNull.Value);

            activity.AuthActivityId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            return activity;
        }

        public async Task<AuthActivity> UpdateAsync(AuthActivity activity)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            NpgsqlCommand cmd;

            if (activity.DeletedBy != null)
            {
                // 🛡️ Soft delete logic
                cmd = new NpgsqlCommand(@"
            UPDATE authactivity SET 
                deletedby = @deletedby,
                deletedon = @deletedon,
                activeflag = @activeflag
            WHERE authdetailid = @authdetailid AND authactivityid = @authactivityid", conn);

                cmd.Parameters.AddWithValue("deletedby", activity.DeletedBy);
                cmd.Parameters.AddWithValue("deletedon", activity.DeletedOn ?? DateTime.UtcNow);
                cmd.Parameters.AddWithValue("activeflag", activity.ActiveFlag ?? false);
                cmd.Parameters.AddWithValue("authdetailid", (object?)activity.AuthDetailId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("authactivityid", activity.AuthActivityId);
            }
            else
            {
                // ✨ Normal full update logic
                cmd = new NpgsqlCommand(@"
            UPDATE authactivity SET 
                authdetailid = @authdetailid, activitytypeid = @activitytypeid, priorityid = @priorityid,
                providerid = @providerid, followupdatetime = @followupdatetime, duedate = @duedate, referredto = @referredto,
                isworkbasket = @isworkbasket, queueid = @queueid, comment = @comment, statusid = @statusid,
                performeddatetime = @performeddatetime, performedby = @performedby, activeflag = @activeflag,
                updatedon = @updatedon, updatedby = @updatedby
            WHERE authactivityid = @authactivityid", conn);

                cmd.Parameters.AddWithValue("authactivityid", activity.AuthActivityId);
                cmd.Parameters.AddWithValue("authdetailid", (object?)activity.AuthDetailId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("activitytypeid", (object?)activity.ActivityTypeId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("priorityid", (object?)activity.PriorityId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("providerid", (object?)activity.ProviderId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("followupdatetime", (object?)activity.FollowUpDateTime ?? DBNull.Value);
                cmd.Parameters.AddWithValue("duedate", (object?)activity.DueDate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("referredto", (object?)activity.ReferredTo ?? DBNull.Value);
                cmd.Parameters.AddWithValue("isworkbasket", (object?)activity.IsWorkBasket ?? DBNull.Value);
                cmd.Parameters.AddWithValue("queueid", (object?)activity.QueueId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("comment", (object?)activity.Comment ?? DBNull.Value);
                cmd.Parameters.AddWithValue("statusid", (object?)activity.StatusId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("performeddatetime", (object?)activity.PerformedDateTime ?? DBNull.Value);
                cmd.Parameters.AddWithValue("performedby", (object?)activity.PerformedBy ?? DBNull.Value);
                cmd.Parameters.AddWithValue("activeflag", (object?)activity.ActiveFlag ?? DBNull.Value);
                cmd.Parameters.AddWithValue("updatedon", activity.UpdatedOn ?? DateTime.UtcNow);
                cmd.Parameters.AddWithValue("updatedby", (object?)activity.UpdatedBy ?? DBNull.Value);
            }

            await cmd.ExecuteNonQueryAsync();
            return activity;
        }



        public async Task<int> CreateMdReviewActivityAsync(MdReviewActivityCreate payload)
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload));
            if (payload.Activity == null) throw new ArgumentNullException(nameof(payload.Activity));

            await using var conn = GetConnection();
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // 1) Insert parent activity (explicitly setting the new columns)
                var insertParentSql = @"
                    INSERT INTO authactivity
                     (authdetailid, activitytypeid, priorityid, providerid, followupdatetime, duedate,
                      referredto, isworkbasket, queueid, comment, statusid, performeddatetime, performedby,
                      activeflag, createdon, createdby,
                      service_line_count, md_review_status, md_aggregate_decision, payload_snapshot_json)
                    VALUES
                     (@authdetailid, @activitytypeid, @priorityid, @providerid, @followupdatetime, @duedate,
                      @referredto, @isworkbasket, @queueid, @comment, @statusid, @performeddatetime, @performedby,
                      @activeflag, @createdon, @createdby,
                      @service_line_count, @md_review_status, @md_aggregate_decision, @payload_snapshot_json)
                    RETURNING authactivityid;";

                await using (var cmd = new NpgsqlCommand(insertParentSql, conn, tx))
                {
                    var a = payload.Activity;

                    cmd.Parameters.AddWithValue("authdetailid", (object?)a.AuthDetailId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("activitytypeid", (object?)a.ActivityTypeId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("priorityid", (object?)a.PriorityId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("providerid", (object?)a.ProviderId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("followupdatetime", (object?)a.FollowUpDateTime ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("duedate", (object?)a.DueDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("referredto", (object?)a.ReferredTo ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("isworkbasket", (object?)a.IsWorkBasket ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("queueid", (object?)a.QueueId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("comment", (object?)a.Comment ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("statusid", (object?)a.StatusId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("performeddatetime", (object?)a.PerformedDateTime ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("performedby", (object?)a.PerformedBy ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("activeflag", (object?)a.ActiveFlag ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("createdon", a.CreatedOn);
                    cmd.Parameters.AddWithValue("createdby", (object?)a.CreatedBy ?? DBNull.Value);

                    // New columns (initialize rollup as Pending; count is set after child insert)
                    cmd.Parameters.AddWithValue("service_line_count", 0);
                    cmd.Parameters.AddWithValue("md_review_status", (object?)"Pending" ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("md_aggregate_decision", (object?)"Pending" ?? DBNull.Value);

                    var snapParam = cmd.Parameters.Add("payload_snapshot_json", NpgsqlDbType.Jsonb);
                    snapParam.Value = (object?)payload.PayloadSnapshotJson ?? DBNull.Value;

                    payload.Activity.AuthActivityId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }

                // 2) Insert child lines
                if (payload.Lines.Count > 0)
                {
                    const string insertLineSql = @"
                        INSERT INTO authactivityline
                         (activityid, decisionlineid, servicecode, description, fromdate, todate,
                          requested, approved, denied, initialrecommendation,
                          status, mddecision, mdnotes, reviewedbyuserid, reviewedon, updatedon, version)
                        VALUES
                         (@activityid, @decisionlineid, @servicecode, @description, @fromdate, @todate,
                          @requested, @approved, @denied, @initialrecommendation,
                          'Pending', 'NotReviewed', NULL, NULL, NULL, now(), 1);";

                    foreach (var l in payload.Lines)
                    {
                        await using var lineCmd = new NpgsqlCommand(insertLineSql, conn, tx);
                        lineCmd.Parameters.AddWithValue("activityid", payload.Activity.AuthActivityId);
                        lineCmd.Parameters.AddWithValue("decisionlineid", (object?)l.DecisionLineId ?? DBNull.Value);
                        lineCmd.Parameters.AddWithValue("servicecode", (object?)l.ServiceCode ?? DBNull.Value);
                        lineCmd.Parameters.AddWithValue("description", (object?)l.Description ?? DBNull.Value);
                        lineCmd.Parameters.AddWithValue("fromdate", (object?)l.FromDate ?? DBNull.Value);
                        lineCmd.Parameters.AddWithValue("todate", (object?)l.ToDate ?? DBNull.Value);
                        lineCmd.Parameters.AddWithValue("requested", (object?)l.Requested ?? DBNull.Value);
                        lineCmd.Parameters.AddWithValue("approved", (object?)l.Approved ?? DBNull.Value);
                        lineCmd.Parameters.AddWithValue("denied", (object?)l.Denied ?? DBNull.Value);
                        lineCmd.Parameters.AddWithValue("initialrecommendation", (object?)l.InitialRecommendation ?? DBNull.Value);

                        await lineCmd.ExecuteNonQueryAsync();
                    }
                }

                // 3) Recompute rollup on parent
                await RecomputeMdReviewRollupInTxAsync(conn, tx, payload.Activity.AuthActivityId);

                await tx.CommitAsync();
                return payload.Activity.AuthActivityId;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> UpdateMdReviewLineAsync(
            long activityId,
            long lineId,
            string mdDecision,           // "Approved" | "Denied" | "Partial"
            string status,               // "Pending" | "InProgress" | "Completed"
            string? mdNotes,
            int? reviewedByUserId,
            long? expectedVersion = null // pass the current version to avoid lost updates
        )
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                var updateSql = @"
                    UPDATE authactivityline
                    SET mddecision = @mddecision,
                        status     = @status,
                        mdnotes    = @mdnotes,
                        reviewedbyuserid = @reviewedbyuserid,
                        reviewedon = CASE WHEN @status = 'Completed' THEN now() ELSE reviewedon END,
                        updatedon  = now(),
                        version    = version + 1
                    WHERE id = @id AND activityid = @activityid
                    " + (expectedVersion.HasValue ? "AND version = @expectedVersion" : "") + @";
                    ";

                await using (var cmd = new NpgsqlCommand(updateSql, conn, tx))
                {
                    cmd.Parameters.AddWithValue("mddecision", mdDecision);
                    cmd.Parameters.AddWithValue("status", status);
                    cmd.Parameters.AddWithValue("mdnotes", (object?)mdNotes ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("reviewedbyuserid", (object?)reviewedByUserId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("id", lineId);
                    cmd.Parameters.AddWithValue("activityid", activityId);
                    if (expectedVersion.HasValue)
                        cmd.Parameters.AddWithValue("expectedVersion", expectedVersion.Value);

                    var affected = await cmd.ExecuteNonQueryAsync();
                    if (affected == 0)
                    {
                        // Either not found OR version mismatch
                        await tx.RollbackAsync();
                        return false;
                    }
                }

                await RecomputeMdReviewRollupInTxAsync(conn, tx, (int)activityId);
                await tx.CommitAsync();
                return true;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }


        public async Task<List<(AuthActivity Activity, List<dynamic> Lines)>> GetMdReviewActivitiesAsync(int? activityId = null, int? authDetailId = null)
        {
            var results = new List<(AuthActivity, List<dynamic>)>();

            await using var conn = GetConnection();
            await conn.OpenAsync();

            // --- Build WHERE clause dynamically ---
            var whereClauses = new List<string> { "deletedon IS NULL" };
            if (activityId.HasValue)
                whereClauses.Add("authactivityid = @activityid");
            if (authDetailId.HasValue)
                whereClauses.Add("authdetailid = @authdetailid");

            var whereSql = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";

            // --- Parent query ---
            var parentSql = $@"
                SELECT *
                FROM authactivity
                {whereSql}
                ORDER BY createdon DESC;";

            var parentCmd = new NpgsqlCommand(parentSql, conn);
            if (activityId.HasValue)
                parentCmd.Parameters.AddWithValue("activityid", activityId.Value);
            if (authDetailId.HasValue)
                parentCmd.Parameters.AddWithValue("authdetailid", authDetailId.Value);

            var parentList = new List<AuthActivity>();
            await using (var reader = await parentCmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    parentList.Add(new AuthActivity
                    {
                        AuthActivityId = reader.GetInt32(reader.GetOrdinal("authactivityid")),
                        AuthDetailId = reader["authdetailid"] as int?,
                        ActivityTypeId = reader["activitytypeid"] as int?,
                        PriorityId = reader["priorityid"] as int?,
                        ProviderId = reader["providerid"] as int?,
                        FollowUpDateTime = reader["followupdatetime"] as DateTime?,
                        DueDate = reader["duedate"] as DateTime?,
                        ReferredTo = reader["referredto"] as int?,
                        IsWorkBasket = reader["isworkbasket"] as bool?,
                        QueueId = reader["queueid"] as int?,
                        Comment = reader["comment"] as string,
                        StatusId = reader["statusid"] as int?,
                        PerformedDateTime = reader["performeddatetime"] as DateTime?,
                        PerformedBy = reader["performedby"] as int?,
                        ActiveFlag = reader["activeflag"] as bool?,
                        CreatedOn = reader.GetDateTime(reader.GetOrdinal("createdon")),
                        CreatedBy = reader["createdby"] as int?,
                        UpdatedOn = reader["updatedon"] as DateTime?,
                        UpdatedBy = reader["updatedby"] as int?,
                        DeletedOn = reader["deletedon"] as DateTime?,
                        DeletedBy = reader["deletedby"] as int?,
                        // new MD review fields
                        ServiceLineCount = reader["service_line_count"] as int?,
                        MdReviewStatus = reader["md_review_status"] as string,
                        MdAggregateDecision = reader["md_aggregate_decision"] as string,
                        PayloadSnapshotJson = reader["payload_snapshot_json"] as string
                    });
                }
            }

            // --- For each parent, pull its lines ---
            const string lineSql = @"
                SELECT id, activityid, decisionlineid, servicecode, description, fromdate, todate,
                       requested, approved, denied, initialrecommendation,
                       status, mddecision, mdnotes, reviewedbyuserid, reviewedon, updatedon, version
                FROM authactivityline
                WHERE activityid = @activityid
                ORDER BY id;";

            foreach (var parent in parentList)
            {
                var lines = new List<dynamic>();
                await using (var lcmd = new NpgsqlCommand(lineSql, conn))
                {
                    lcmd.Parameters.AddWithValue("activityid", parent.AuthActivityId);
                    await using var lr = await lcmd.ExecuteReaderAsync();
                    while (await lr.ReadAsync())
                    {
                        lines.Add(new
                        {
                            Id = lr.GetInt64(lr.GetOrdinal("id")),
                            ActivityId = lr.GetInt64(lr.GetOrdinal("activityid")),
                            DecisionLineId = lr["decisionlineid"] as long?,
                            ServiceCode = lr["servicecode"] as string,
                            Description = lr["description"] as string,
                            FromDate = lr["fromdate"] as DateTime?,
                            ToDate = lr["todate"] as DateTime?,
                            Requested = lr["requested"] as int?,
                            Approved = lr["approved"] as int?,
                            Denied = lr["denied"] as int?,
                            InitialRecommendation = lr["initialrecommendation"] as string,
                            Status = lr["status"] as string,
                            MdDecision = lr["mddecision"] as string,
                            MdNotes = lr["mdnotes"] as string,
                            ReviewedByUserId = lr["reviewedbyuserid"] as int?,
                            ReviewedOn = lr["reviewedon"] as DateTime?,
                            UpdatedOn = lr.GetDateTime(lr.GetOrdinal("updatedon")),
                            Version = lr.GetInt64(lr.GetOrdinal("version"))
                        });
                    }
                }

                results.Add((parent, lines));
            }

            return results;
        }


        private static string BuildRollupUpdateSql() => @"
                WITH agg AS (
                  SELECT
                    activityid,
                    COUNT(*) AS total,
                    COUNT(*) FILTER (WHERE status = 'Completed') AS completed,
                    COUNT(*) FILTER (WHERE status <> 'Completed') AS not_completed,
                    COUNT(*) FILTER (WHERE mddecision = 'Approved' AND status = 'Completed') AS approved,
                    COUNT(*) FILTER (WHERE mddecision = 'Denied'   AND status = 'Completed') AS denied
                  FROM authactivityline
                  WHERE activityid = @activityid
                  GROUP BY activityid
                )
                UPDATE authactivity a
                SET service_line_count    = agg.total,
                    md_review_status      = CASE
                                              WHEN agg.completed = agg.total THEN 'Completed'
                                              WHEN agg.completed > 0 THEN 'InProgress'
                                              ELSE 'Pending'
                                            END,
                    md_aggregate_decision = CASE
                                              WHEN agg.completed = 0 THEN 'Pending'
                                              WHEN agg.approved = agg.total THEN 'Approved'
                                              WHEN agg.denied   = agg.total THEN 'Denied'
                                              ELSE 'Mixed'
                                            END
                FROM agg
                WHERE a.authactivityid = agg.activityid;";

        private async Task RecomputeMdReviewRollupInTxAsync(NpgsqlConnection conn, NpgsqlTransaction tx, int activityId)
        {
            var sql = BuildRollupUpdateSql();
            await using var cmd = new NpgsqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("activityid", activityId);
            await cmd.ExecuteNonQueryAsync();
        }


    }


}
