using CareNirvana.Service.Domain.Model;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Dapper;
using System.Collections.Generic;
using System.Threading.Tasks;
using CareNirvana.Service.Application.Interfaces;


namespace CareNirvana.Service.Infrastructure.Repository
{
    public class WorkBasketRepository : IWorkBasket
    {
        private readonly string _connectionString;

        public WorkBasketRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        private NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }


        public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
        {
            const string sql = @"
                SELECT EXISTS(
                  SELECT 1 FROM public.cfgworkbasket
                  WHERE LOWER(workbasketname)=LOWER(@name)
                    AND (@excludeId IS NULL OR workbasketid<>@excludeId)
                );";
            await using var conn = GetConnection();
            return await conn.ExecuteScalarAsync<bool>(sql, new { name, excludeId });
        }

        public async Task<bool> ExistsByCodeAsync(string code, int? excludeId = null)
        {
            const string sql = @"
                SELECT EXISTS(
                  SELECT 1 FROM public.cfgworkbasket
                  WHERE LOWER(workbasketcode)=LOWER(@code)
                    AND (@excludeId IS NULL OR workbasketid<>@excludeId)
                );";
            await using var conn = GetConnection();
            return await conn.ExecuteScalarAsync<bool>(sql, new { code, excludeId });
        }

        public async Task<int> CreateWithGroupsAsync(WorkBasket e, IEnumerable<int> workGroupIds)
        {
            const string insertBasket = @"
                INSERT INTO public.cfgworkbasket
                ( workbasketcode, workbasketname, description, activeflag, createdby, createdon )
                VALUES
                ( @WorkBasketCode, @WorkBasketName, @Description, COALESCE(@ActiveFlag,true), @CreatedBy, @CreatedOn )
                RETURNING workbasketid;";

            const string insertLink = @"
                INSERT INTO public.cfgworkgroupworkbasket
                ( workgroupid, workbasketid, activeflag, createdby, createdon )
                VALUES ( @WorkGroupId, @WorkBasketId, true, @CreatedBy, @CreatedOn );";

            await using var conn = GetConnection();
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                var now = DateTimeOffset.UtcNow;

                var id = await conn.ExecuteScalarAsync<int>(insertBasket, new
                {
                    e.WorkBasketCode,
                    e.WorkBasketName,
                    e.Description,
                    e.ActiveFlag,
                    e.CreatedBy,
                    CreatedOn = now
                }, tx);

                foreach (var gid in workGroupIds.Distinct())
                {
                    await conn.ExecuteAsync(insertLink, new
                    {
                        WorkGroupId = gid,
                        WorkBasketId = id,
                        CreatedBy = e.CreatedBy,
                        CreatedOn = now
                    }, tx);
                }

                await tx.CommitAsync();
                return id;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<int> UpdateWithGroupsAsync(WorkBasket e, IEnumerable<int> workGroupIds)
        {
            const string updateBasket = @"
                UPDATE public.cfgworkbasket
                SET workbasketcode = @WorkBasketCode,
                    workbasketname = @WorkBasketName,
                    description = @Description,
                    activeflag = @ActiveFlag,
                    updatedby = @UpdatedBy,
                    updatedon = @UpdatedOn
                WHERE workbasketid = @WorkBasketId;";

            const string selectExisting = @"
                SELECT workgroupid, activeflag
                FROM public.cfgworkgroupworkbasket
                WHERE workbasketid=@WorkBasketId;";

            const string insertLink = @"
                INSERT INTO public.cfgworkgroupworkbasket
                ( workgroupid, workbasketid, activeflag, createdby, createdon )
                VALUES ( @WorkGroupId, @WorkBasketId, true, @By, @On );";

            const string reactivateLink = @"
                UPDATE public.cfgworkgroupworkbasket
                SET activeflag=true, updatedby=@By, updatedon=@On, deletedby=NULL, deletedon=NULL
                WHERE workbasketid=@WorkBasketId AND workgroupid=@WorkGroupId AND activeflag=false;";

            const string softDeleteLink = @"
                UPDATE public.cfgworkgroupworkbasket
                SET activeflag=false, deletedby=@By, deletedon=@On
                WHERE workbasketid=@WorkBasketId AND workgroupid=@WorkGroupId AND activeflag=true;";

            await using var conn = GetConnection();
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                var now = DateTimeOffset.UtcNow;

                // 1) Update basket
                var rows = await conn.ExecuteAsync(updateBasket, new
                {
                    e.WorkBasketCode,
                    e.WorkBasketName,
                    e.Description,
                    e.ActiveFlag,
                    e.UpdatedBy,
                    UpdatedOn = now,
                    e.WorkBasketId
                }, tx);
                if (rows == 0) return 0;

                // 2) Reconcile links
                var existing = (await conn.QueryAsync<(int WorkGroupId, bool ActiveFlag)>(selectExisting, new { e.WorkBasketId }, tx)).ToList();

                var desired = new HashSet<int>(workGroupIds ?? Enumerable.Empty<int>());
                var existingIds = existing.Select(x => x.WorkGroupId).ToHashSet();

                // to insert (new)
                var toInsert = desired.Except(existingIds);
                foreach (var gid in toInsert)
                {
                    await conn.ExecuteAsync(insertLink, new { WorkGroupId = gid, e.WorkBasketId, By = e.UpdatedBy, On = now }, tx);
                }

                // to reactivate (was inactive but present)
                var toReactivate = existing.Where(x => desired.Contains(x.WorkGroupId) && x.ActiveFlag == false).Select(x => x.WorkGroupId);
                foreach (var gid in toReactivate)
                {
                    await conn.ExecuteAsync(reactivateLink, new { e.WorkBasketId, WorkGroupId = gid, By = e.UpdatedBy, On = now }, tx);
                }

                // to soft delete (present active but not desired)
                var toSoftDelete = existing.Where(x => !desired.Contains(x.WorkGroupId) && x.ActiveFlag == true).Select(x => x.WorkGroupId);
                foreach (var gid in toSoftDelete)
                {
                    await conn.ExecuteAsync(softDeleteLink, new { e.WorkBasketId, WorkGroupId = gid, By = e.UpdatedBy, On = now }, tx);
                }

                await tx.CommitAsync();
                return rows;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<WorkBasket?> GetByIdAsync(int id)
        {
            const string sql = @"
                    SELECT
                      workbasketid   AS WorkBasketId,
                      workbasketcode AS WorkBasketCode,
                      workbasketname AS WorkBasketName,
                      description    AS Description,
                      activeflag     AS ActiveFlag,
                      createdby      AS CreatedBy,
                      createdon      AS CreatedOn,
                      updatedby      AS UpdatedBy,
                      updatedon      AS UpdatedOn,
                      deletedby      AS DeletedBy,
                      deletedon      AS DeletedOn
                    FROM public.cfgworkbasket
                    WHERE workbasketid=@id;";

            await using var conn = GetConnection();
            return await conn.QueryFirstOrDefaultAsync<WorkBasket>(sql, new { id });
        }

        public async Task<IEnumerable<WorkBasket>> GetAllAsync(bool includeInactive = false)
        {
            const string sql = @"
                SELECT
                      workbasketid   AS WorkBasketId,
                      workbasketcode AS WorkBasketCode,
                      workbasketname AS WorkBasketName,
                      description    AS Description,
                      activeflag     AS ActiveFlag,
                      createdby      AS CreatedBy,
                      createdon      AS CreatedOn,
                      updatedby      AS UpdatedBy,
                      updatedon      AS UpdatedOn,
                      deletedby      AS DeletedBy,
                      deletedon      AS DeletedOn
                    FROM public.cfgworkbasket
                    WHERE (@includeInactive=true) OR activeflag=true
                    ORDER BY workbasketname;";

            await using var conn = GetConnection();
            return await conn.QueryAsync<WorkBasket>(sql, new { includeInactive });
        }

        public async Task<List<int>> GetLinkedWorkGroupIdsAsync(int workBasketId)
        {
            const string sql = @"
                SELECT workgroupid
                FROM public.cfgworkgroupworkbasket
                WHERE workbasketid=@id AND activeflag=true
                ORDER BY workgroupid;";

            await using var conn = GetConnection();
            var ids = await conn.QueryAsync<int>(sql, new { id = workBasketId });
            return ids.ToList();
        }

        public async Task<int> SoftDeleteAsync(int id, string deletedBy)
        {
            const string softBasket = @"
                UPDATE public.cfgworkbasket
                SET activeflag=false, deletedby=@By, deletedon=@On
                WHERE workbasketid=@Id;";

            const string softLinks = @"
                UPDATE public.cfgworkgroupworkbasket
                SET activeflag=false, deletedby=@By, deletedon=@On
                WHERE workbasketid=@Id AND activeflag=true;";

            await using var conn = GetConnection();
            await using var tx = await conn.BeginTransactionAsync();

            var now = DateTimeOffset.UtcNow;
            var a = await conn.ExecuteAsync(softBasket, new { Id = id, By = deletedBy, On = now }, tx);
            var b = await conn.ExecuteAsync(softLinks, new { Id = id, By = deletedBy, On = now }, tx);

            await tx.CommitAsync();
            return a; // rows affected on basket
        }

        public async Task<int> HardDeleteAsync(int id)
        {
            // links will be cascaded if you hard delete basket (because FK CASCADE),
            // but we still delete links first for clarity.
            const string delLinks = @"DELETE FROM public.cfgworkgroupworkbasket WHERE workbasketid=@Id;";
            const string delBasket = @"DELETE FROM public.cfgworkbasket WHERE workbasketid=@Id;";

            await using var conn = GetConnection();
            await using var tx = await conn.BeginTransactionAsync();

            await conn.ExecuteAsync(delLinks, new { Id = id }, tx);
            var rows = await conn.ExecuteAsync(delBasket, new { Id = id }, tx);

            await tx.CommitAsync();
            return rows;
        }

        public async Task<IEnumerable<UserWorkGroupAssignment>> GetUserWorkGroupsAsync(int userId)
        {
            const string sql = @"
                WITH my_groups AS (
                  SELECT DISTINCT wgb.workgroupid
                  FROM public.cfguserworkgroup ugw
                  JOIN public.cfgworkgroupworkbasket wgb
                    ON wgb.workgroupworkbasketid = ugw.workgroupworkbasketid
                  WHERE ugw.userid = @UserId
                    AND ugw.deletedon IS NULL
                    AND wgb.deletedon IS NULL
                ),
                group_baskets AS (
                  SELECT DISTINCT wgb.workgroupid, wgb.workbasketid
                  FROM public.cfgworkgroupworkbasket wgb
                  JOIN my_groups g ON g.workgroupid = wgb.workgroupid
                  WHERE wgb.deletedon IS NULL
                ),
                users_via_ugw AS (  -- users assigned via any workgroup-workbasket row for the same basket
                  SELECT DISTINCT
                         gb.workgroupid,
                         gb.workbasketid,
                         su.userid,
                         su.username,
                         ugw.workgroupworkbasketid,
                         ugw.userworkgroupid,
                         ugw.activeflag
                  FROM group_baskets gb
                  JOIN public.cfgworkgroupworkbasket wgb
                    ON wgb.workbasketid = gb.workbasketid
                   AND wgb.deletedon IS NULL
                  JOIN public.cfguserworkgroup ugw
                    ON ugw.workgroupworkbasketid = wgb.workgroupworkbasketid
                   AND ugw.deletedon IS NULL
                  JOIN public.securityuser su
                    ON su.userid = ugw.userid
                ),
                all_users AS (
                  SELECT * FROM users_via_ugw
                )
                SELECT
                  au.userworkgroupid                 AS ""UserWorkGroupId"",
                  au.userid                          AS ""UserId"",
                  au.username                        AS ""UserFullName"",
                  au.workgroupworkbasketid           AS ""WorkGroupWorkBasketId"",
                  wg.workgroupid                     AS ""WorkGroupId"",
                  wg.workgroupcode                   AS ""WorkGroupCode"",
                  wg.workgroupname                   AS ""WorkGroupName"",
                  wb.workbasketid                    AS ""WorkBasketId"",
                  wb.workbasketcode                  AS ""WorkBasketCode"",
                  wb.workbasketname                  AS ""WorkBasketName"",
                  au.activeflag                      AS ""ActiveFlag""
                FROM all_users au
                JOIN public.cfgworkgroup  wg ON wg.workgroupid  = au.workgroupid
                JOIN public.cfgworkbasket wb ON wb.workbasketid = au.workbasketid
                ORDER BY wg.workgroupname, wb.workbasketname, au.username;";


            await using var conn = GetConnection(); // your existing Npgsql connection factory
            return await conn.QueryAsync<UserWorkGroupAssignment>(sql, new { UserId = userId });
        }

    }

}
