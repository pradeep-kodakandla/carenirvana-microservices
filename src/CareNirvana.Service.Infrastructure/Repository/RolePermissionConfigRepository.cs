using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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
    }
}
