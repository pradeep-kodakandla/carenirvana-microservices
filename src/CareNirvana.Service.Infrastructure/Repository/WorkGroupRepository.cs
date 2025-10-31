using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;


namespace CareNirvana.Service.Infrastructure.Repository
{
    public class WorkGroupRepository : IWorkGroup
    {
        private readonly string _connectionString;

        public WorkGroupRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        private NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }
        public async Task<int> CreateAsync(WorkGroup e)
        {
            const string sql = @"
                INSERT INTO public.cfgworkgroup
                ( workgroupcode, workgroupname, description, isfax, isproviderportal, activeflag,
                  createdby, createdon )
                VALUES
                ( @WorkGroupCode, @WorkGroupName, @Description, @IsFax, @IsProviderPortal, COALESCE(@ActiveFlag, true),
                  @CreatedBy, @CreatedOn )
                RETURNING workgroupid;";

            await using var conn = GetConnection();
            var id = await conn.ExecuteScalarAsync<int>(sql, new
            {
                e.WorkGroupCode,
                e.WorkGroupName,
                e.Description,
                e.IsFax,
                e.IsProviderPortal,
                e.ActiveFlag,
                e.CreatedBy,
                CreatedOn = DateTimeOffset.UtcNow
            });

            return id;
        }

        public async Task<WorkGroup?> GetByIdAsync(int workGroupId)
        {
            const string sql = @"
                SELECT
                  workgroupid AS WorkGroupId,
                  workgroupcode AS WorkGroupCode,
                  workgroupname AS WorkGroupName,
                  description AS Description,
                  isfax AS IsFax,
                  isproviderportal AS IsProviderPortal,
                  activeflag AS ActiveFlag,
                  createdby AS CreatedBy,
                  createdon AS CreatedOn,
                  updatedby AS UpdatedBy,
                  updatedon AS UpdatedOn,
                  deletedby AS DeletedBy,
                  deletedon AS DeletedOn
                FROM public.cfgworkgroup
                WHERE workgroupid = @workgroupid;";

            await using var conn = GetConnection();
            return await conn.QueryFirstOrDefaultAsync<WorkGroup>(sql, new { workgroupid = workGroupId });
        }

        public async Task<IEnumerable<WorkGroup>> GetAllAsync(bool includeInactive = false)
        {
            const string sql = @"
                SELECT
                  workgroupid AS WorkGroupId,
                  workgroupcode AS WorkGroupCode,
                  workgroupname AS WorkGroupName,
                  description AS Description,
                  isfax AS IsFax,
                  isproviderportal AS IsProviderPortal,
                  activeflag AS ActiveFlag,
                  createdby AS CreatedBy,
                  createdon AS CreatedOn,
                  updatedby AS UpdatedBy,
                  updatedon AS UpdatedOn,
                  deletedby AS DeletedBy,
                  deletedon AS DeletedOn
                FROM public.cfgworkgroup
                WHERE (@includeInactive = true) OR activeflag = true
                ORDER BY workgroupname;";

            await using var conn = GetConnection();
            return await conn.QueryAsync<WorkGroup>(sql, new { includeInactive });
        }

        public async Task<bool> ExistsByNameAsync(string workGroupName, int? excludeId = null)
        {
            const string sql = @"
                SELECT EXISTS(
                  SELECT 1 FROM public.cfgworkgroup
                  WHERE LOWER(workgroupname) = LOWER(@name)
                    AND (@excludeId IS NULL OR workgroupid <> @excludeId)
                );";

            await using var conn = GetConnection();
            return await conn.ExecuteScalarAsync<bool>(sql, new { name = workGroupName, excludeId });
        }

        public async Task<bool> ExistsByCodeAsync(string workGroupCode, int? excludeId = null)
        {
            const string sql = @"
                SELECT EXISTS(
                    SELECT 1 FROM public.cfgworkgroup
                    WHERE LOWER(workgroupcode) = LOWER(@code)
                    AND (@excludeId IS NULL OR workgroupid <> @excludeId)
                );";

            await using var conn = GetConnection();
            return await conn.ExecuteScalarAsync<bool>(sql, new { code = workGroupCode, excludeId });
        }

        public async Task<int> UpdateAsync(WorkGroup e)
        {
            const string sql = @"
                UPDATE public.cfgworkgroup
                SET workgroupcode = @WorkGroupCode,
                    workgroupname = @WorkGroupName,
                    description = @Description,
                    isfax = @IsFax,
                    isproviderportal = @IsProviderPortal,
                    activeflag = @ActiveFlag,
                    updatedby = @UpdatedBy,
                    updatedon = @UpdatedOn
                WHERE workgroupid = @WorkGroupId;";

            await using var conn = GetConnection();
            return await conn.ExecuteAsync(sql, new
            {
                e.WorkGroupCode,
                e.WorkGroupName,
                e.Description,
                e.IsFax,
                e.IsProviderPortal,
                e.ActiveFlag,
                e.WorkGroupId,
                e.UpdatedBy,
                UpdatedOn = DateTimeOffset.UtcNow
            });
        }

        public async Task<int> SoftDeleteAsync(int workgroupId, string deletedBy)
        {
            const string sql = @"
                UPDATE public.cfgworkgroup
                SET activeflag = false,
                    deletedby = @deletedBy,
                    deletedon = @deletedOn
                WHERE workgroupid = @workgroupId;";

            await using var conn = GetConnection();
            return await conn.ExecuteAsync(sql, new
            {
                workgroupId,
                deletedBy,
                deletedOn = DateTimeOffset.UtcNow
            });
        }

        public async Task<int> RestoreAsync(int workgroupId, string updatedBy)
        {
            const string sql = @"
                UPDATE public.cfgworkgroup
                SET activeflag = true,
                    updatedby = @updatedBy,
                    updatedon = @updatedOn,
                    deletedby = NULL,
                    deletedon = NULL
                WHERE workgroupid = @workgroupId;";

            await using var conn = GetConnection();
            return await conn.ExecuteAsync(sql, new
            {
                workgroupId,
                updatedBy,
                updatedOn = DateTimeOffset.UtcNow
            });
        }

        public async Task<int> HardDeleteAsync(int workgroupId)
        {
            const string sql = @"DELETE FROM public.cfgworkgroup WHERE workgroupid = @workgroupId;";
            await using var conn = GetConnection();
            return await conn.ExecuteAsync(sql, new { workgroupId });
        }
    }
}

