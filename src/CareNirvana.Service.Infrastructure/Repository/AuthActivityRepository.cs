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

    }

}
