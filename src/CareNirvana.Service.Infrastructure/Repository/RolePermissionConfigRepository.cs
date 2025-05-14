using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace CareNirvana.Service.Infrastructure.Repository
{
    public class RolePermissionConfigRepository : IRolePermissionConfigRepository
    {
        private readonly string _connectionString;

        public RolePermissionConfigRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        private NpgsqlConnection GetConnection() => new NpgsqlConnection(_connectionString);

        public async Task<IEnumerable<CfgModule>> GetModulesAsync()
        {
            var list = new List<CfgModule>();

            using var conn = GetConnection();
            await conn.OpenAsync();

            var cmd = new NpgsqlCommand("SELECT moduleid, modulename FROM cfgmodule WHERE activeflag = true", conn);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new CfgModule
                {
                    ModuleId = reader.GetInt32(0),
                    ModuleName = reader.GetString(1)
                });
            }

            return list;
        }

        public async Task<IEnumerable<CfgFeatureGroup>> GetFeatureGroupsByModuleAsync(int moduleId)
        {
            var list = new List<CfgFeatureGroup>();

            using var conn = GetConnection();
            await conn.OpenAsync();

            var cmd = new NpgsqlCommand(@"
                SELECT fg.featuregroupid, fg.featuregroupname
                FROM cfgmodulefeaturegroup mfg
                JOIN cfgfeaturegroup fg ON fg.featuregroupid = mfg.featuregroupid
                WHERE mfg.moduleid = @moduleId AND fg.activeflag = true", conn);

            cmd.Parameters.AddWithValue("moduleId", moduleId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new CfgFeatureGroup
                {
                    FeatureGroupId = reader.GetInt32(0),
                    FeatureGroupName = reader.GetString(1)
                });
            }

            return list;
        }

        public async Task<IEnumerable<CfgFeature>> GetFeaturesByFeatureGroupAsync(int featureGroupId)
        {
            var list = new List<CfgFeature>();

            using var conn = GetConnection();
            await conn.OpenAsync();

            var cmd = new NpgsqlCommand(@"
                SELECT f.featureid, f.featurename
                FROM cfgfeaturegroupfeature fgf
                JOIN cfgfeature f ON f.featureid = fgf.featureid
                WHERE fgf.featuregroupid = @featureGroupId AND f.activeflag = true", conn);

            cmd.Parameters.AddWithValue("featureGroupId", featureGroupId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new CfgFeature
                {
                    FeatureId = reader.GetInt32(0),
                    FeatureName = reader.GetString(1)
                });
            }

            return list;
        }

        public async Task<IEnumerable<CfgResource>> GetResourcesByFeatureAsync(int featureId)
        {
            var list = new List<CfgResource>();

            using var conn = GetConnection();
            await conn.OpenAsync();

            var cmd = new NpgsqlCommand(@"
                SELECT fr.featureresourceid, r.resourceid, r.resourcename, 
                       fr.allow_view, fr.allow_add, fr.allow_edit, fr.allow_delete, 
                       fr.allow_print, fr.allow_download
                FROM cfgfeatureresource fr
                JOIN cfgresource r ON fr.resourceid = r.resourceid
                WHERE fr.featureid = @featureId AND fr.activeflag = true", conn);

            cmd.Parameters.AddWithValue("featureId", featureId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new CfgResource
                {
                    FeatureResourceId = reader.GetInt32(0),
                    ResourceId = reader.GetInt32(1),
                    ResourceName = reader.GetString(2),
                    AllowView = reader.GetBoolean(3),
                    AllowAdd = reader.GetBoolean(4),
                    AllowEdit = reader.GetBoolean(5),
                    AllowDelete = reader.GetBoolean(6),
                    AllowPrint = reader.GetBoolean(7),
                    AllowDownload = reader.GetBoolean(8)
                });
            }

            return list;
        }


        /********Role Config********/
        public async Task<IEnumerable<CfgRole>> GetAllAsync()
        {
            var list = new List<CfgRole>();
            var sql = "SELECT * FROM public.cfgrole WHERE deleted_on IS NULL ORDER BY role_id";

            using var conn = GetConnection();
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new CfgRole
                {
                    RoleId = reader.GetInt32(reader.GetOrdinal("role_id")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    ManagerAccess = reader.GetString(reader.GetOrdinal("manager_access")),
                    QocAccess = reader.GetString(reader.GetOrdinal("qoc_access")),
                    Sensitive = reader.GetString(reader.GetOrdinal("sensitive")),
                    Permissions = reader.IsDBNull(reader.GetOrdinal("permissions")) ? null : reader.GetString(reader.GetOrdinal("permissions")),
                    CreatedBy = reader.IsDBNull(reader.GetOrdinal("created_by")) ? null : reader.GetInt32(reader.GetOrdinal("created_by")),
                    CreatedOn = reader.IsDBNull(reader.GetOrdinal("created_on")) ? null : reader.GetDateTime(reader.GetOrdinal("created_on"))
                });
            }

            return list;
        }


        public async Task<CfgRole?> GetByIdAsync(int roleId)
        {
            var sql = "SELECT * FROM public.cfgrole WHERE role_id = @roleId";

            using var conn = GetConnection();
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("roleId", roleId);

            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new CfgRole
                {
                    RoleId = reader.GetInt32(reader.GetOrdinal("role_id")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    ManagerAccess = reader.GetString(reader.GetOrdinal("manager_access")),
                    QocAccess = reader.GetString(reader.GetOrdinal("qoc_access")),
                    Sensitive = reader.GetString(reader.GetOrdinal("sensitive")),
                    Permissions = reader.IsDBNull(reader.GetOrdinal("permissions")) ? null : reader.GetString(reader.GetOrdinal("permissions")),
                    CreatedBy = reader.IsDBNull(reader.GetOrdinal("created_by")) ? null : reader.GetInt32(reader.GetOrdinal("created_by")),
                    CreatedOn = reader.IsDBNull(reader.GetOrdinal("created_on")) ? null : reader.GetDateTime(reader.GetOrdinal("created_on"))
                };
            }

            return null;
        }


        public async Task<int> AddAsync(CfgRole role)
        {
            var sql = @"
        INSERT INTO public.cfgrole (name, manager_access, qoc_access, sensitive, permissions, created_by)
        VALUES (@name, @manager_access, @qoc_access, @sensitive, @permissions::jsonb, @created_by)
        RETURNING role_id;";

            using var conn = GetConnection();
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("name", role.Name ?? string.Empty);
            cmd.Parameters.AddWithValue("manager_access", role.ManagerAccess ?? string.Empty);
            cmd.Parameters.AddWithValue("qoc_access", role.QocAccess ?? string.Empty);
            cmd.Parameters.AddWithValue("sensitive", role.Sensitive ?? string.Empty);
            cmd.Parameters.AddWithValue("permissions", (object?)role.Permissions ?? DBNull.Value);
            cmd.Parameters.AddWithValue("created_by", role.CreatedBy ?? (object)DBNull.Value);

            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }


        public async Task UpdateAsync(CfgRole role)
        {
            var sql = @"
        UPDATE public.cfgrole SET
            name = @name,
            manager_access = @manager_access,
            qoc_access = @qoc_access,
            sensitive = @sensitive,
            permissions = @permissions::jsonb,
            updated_by = @updated_by,
            updated_on = CURRENT_TIMESTAMP
        WHERE role_id = @role_id";

            using var conn = GetConnection();
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("name", role.Name ?? string.Empty);
            cmd.Parameters.AddWithValue("manager_access", role.ManagerAccess ?? string.Empty);
            cmd.Parameters.AddWithValue("qoc_access", role.QocAccess ?? string.Empty);
            cmd.Parameters.AddWithValue("sensitive", role.Sensitive ?? string.Empty);
            cmd.Parameters.AddWithValue("permissions", (object?)role.Permissions ?? DBNull.Value);
            cmd.Parameters.AddWithValue("updated_by", role.UpdatedBy ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("role_id", role.RoleId);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteAsync(int roleId, int deletedBy)
        {
            var sql = @"
        UPDATE public.cfgrole SET
            deleted_by = @deleted_by,
            deleted_on = CURRENT_TIMESTAMP
        WHERE role_id = @role_id";

            using var conn = GetConnection();
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("role_id", roleId);
            cmd.Parameters.AddWithValue("deleted_by", deletedBy);

            await cmd.ExecuteNonQueryAsync();
        }

    }
}
