using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Text.Json;

namespace CareNirvana.Service.Infrastructure.Repository
{
    public sealed class RulesEngineRepository : IRulesEngineRepository
    {
        private readonly string _cs;

        public RulesEngineRepository(IConfiguration cfg)
        {
            _cs = cfg.GetConnectionString("DefaultConnection") ?? throw new Exception("Missing ConnectionStrings:DefaultConnection");
        }

        private NpgsqlConnection Conn() => new NpgsqlConnection(_cs);

        public async Task<IReadOnlyList<RuleGroupDto>> GetRuleGroupsAsync()
        {
            const string sql = @"
                select
                  rulegroupid as Id,
                  rulegroupname as Name,
                  description,
                  activeflag as ActiveFlag,
                  createdon as CreatedOn,
                  createdby as CreatedBy,
                  updatedon as UpdatedOn,
                  updatedby as UpdatedBy,
                  deletedon as DeletedOn,
                  deletedby as DeletedBy
                from rulesengine.cfgrulegroup
                where deletedon is null
                order by rulegroupid desc;";

            using var db = Conn();
            var rows = await db.QueryAsync<RuleGroupDto>(sql);
            return rows.AsList();
        }


        public async Task<long> CreateRuleGroupAsync(UpsertRuleGroupRequest req, long? userId = null)
        {
            const string sql = @"
        insert into rulesengine.cfgrulegroup (rulegroupname, description, activeflag, createdby)
        values (@Name, @Description, @ActiveFlag, @UserId)
        returning rulegroupid;";

            using var db = Conn();
            return await db.ExecuteScalarAsync<long>(sql, new
            {
                req.Name,
                req.Description,
                req.ActiveFlag,
                UserId = userId
            });
        }


        public async Task UpdateRuleGroupAsync(long id, UpsertRuleGroupRequest req, long? userId = null)
        {
            const string sql = @"
        update rulesengine.cfgrulegroup
        set
          rulegroupname = @Name,
          description = @Description,
          activeflag = @ActiveFlag,
          updatedon = now(),
          updatedby = @UserId
        where rulegroupid = @Id
          and deletedon is null;";

            using var db = Conn();
            await db.ExecuteAsync(sql, new
            {
                Id = id,
                req.Name,
                req.Description,
                req.ActiveFlag,
                UserId = userId
            });
        }


        public async Task SoftDeleteRuleGroupAsync(long id, long? userId = null)
        {
            // soft delete group + soft delete its rules
            const string sql = @"
                update rulesengine.cfgrulegroup
                set activeflag = false, deletedon = now(), deletedby = @UserId
                where rulegroupid = @Id and deletedon is null;

                update rulesengine.ruledef
                set isdeleted = true, deletedat = now(), deletedby = @UserId
                where rulegroupid = @Id and isdeleted = false;";

                    using var db = Conn();
                    await db.ExecuteAsync(sql, new { Id = id, UserId = userId });
        }


        public async Task<IReadOnlyList<RuleDto>> GetRulesAsync(long? ruleGroupId = null)
        {
            const string sql = @"
                select
                  ruleid as Id,
                  rulegroupid as RuleGroupId,
                  rulename as Name,
                  ruletype as RuleType,
                  ruledescription as Description,
                  rulejson::text as RuleJson,
                  activeflag as ActiveFlag,
                  createdon as CreatedOn,
                  createdby as CreatedBy,
                  updatedon as UpdatedOn,
                  updatedby as UpdatedBy,
                  deletedon as DeletedOn,
                  deletedby as DeletedBy
                from rulesengine.cfgrule
                where deletedon is null
                  and (@RuleGroupId is null or rulegroupid = @RuleGroupId)
                order by ruleid desc;";

            using var db = Conn();
            var rows = await db.QueryAsync<RuleDto>(sql, new { RuleGroupId = ruleGroupId });
            return rows.AsList();
        }


        public async Task<long> CreateRuleAsync(UpsertRuleRequest req, long? userId = null)
        {
            const string sql = @"
                insert into rulesengine.cfgrule
                (rulegroupid, rulename, ruletype, ruledescription, rulejson, activeflag, createdby)
                values
                (@RuleGroupId, @Name, @RuleType, @Description, @RuleJson::jsonb, @ActiveFlag, @UserId)
                returning ruleid;";

            using var db = Conn();
            return await db.ExecuteScalarAsync<long>(sql, new
            {
                req.RuleGroupId,
                req.Name,
                req.RuleType,
                req.Description,
                RuleJson = req.RuleJson,
                req.ActiveFlag,
                UserId = userId
            });
        }


        public async Task UpdateRuleAsync(long id, UpsertRuleRequest req, long? userId = null)
        {
            const string sql = @"
                update rulesengine.cfgrule
                set
                  rulegroupid = @RuleGroupId,
                  rulename = @Name,
                  ruletype = @RuleType,
                  ruledescription = @Description,
                  rulejson = @RuleJson::jsonb,
                  activeflag = @ActiveFlag,
                  updatedon = now(),
                  updatedby = @UserId
                where ruleid = @Id
                  and deletedon is null;";

            using var db = Conn();
            await db.ExecuteAsync(sql, new
            {
                Id = id,
                req.RuleGroupId,
                req.Name,
                req.RuleType,
                req.Description,
                RuleJson = req.RuleJson,
                req.ActiveFlag,
                UserId = userId
            });
        }


        public async Task SoftDeleteRuleAsync(long id, long? userId = null)
        {
            const string sql = @"
                update rulesengine.cfgrule
                set activeflag = false, deletedon = now(), deletedby = @UserId
                where ruleid = @Id
                  and deletedon is null;";

            using var db = Conn();
            await db.ExecuteAsync(sql, new { Id = id, UserId = userId });
        }


        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public async Task<IReadOnlyList<DecisionTableListDto>> GetDecisionTablesAsync()
        {
            const string sql = @"
                select
                  uniquedecisiontableid as Id,
                  ruledecisiontablename as Name,
                  deploymentstatus as Status,
                  version,
                  coalesce(updatedon, createdon) as UpdatedOn,
                  activeflag as ActiveFlag
                from rulesengine.cfgruledecisiontable
                where deletedon is null
                order by lower(ruledecisiontablename);";

            using var db = Conn();
            var rows = await db.QueryAsync<DecisionTableListDto>(sql);
            return rows.AsList();
        }


        public async Task<string?> GetDecisionTableJsonAsync(string id)
        {
            const string sql = @"
                select decisiontablejson::text
                from rulesengine.cfgruledecisiontable
                where uniquedecisiontableid = @Id
                  and deletedon is null;";

            using var db = Conn();
            return await db.ExecuteScalarAsync<string?>(sql, new { Id = id });
        }


        public async Task<string> CreateDecisionTableAsync(
            string json,
            string id,
            string name,
            string description,
            string hitPolicy,
            string status,
            int version,
            long? userId = null)
        {
            const string sql = @"
                insert into rulesengine.cfgruledecisiontable
                (uniquedecisiontableid, ruledecisiontablename, description, hitpolicy, deploymentstatus, version, decisiontablejson, activeflag, createdby)
                values
                (@Id, @Name, @Description, @HitPolicy, @Status, @Version, @Json::jsonb, true, @UserId);";

            using var db = Conn();
            await db.ExecuteAsync(sql, new
            {
                Id = id,
                Name = name,
                Description = description ?? "",
                HitPolicy = hitPolicy,
                Status = status,
                Version = version,
                Json = json,
                UserId = userId
            });

            return id;
        }


        public async Task UpdateDecisionTableAsync(
             string id,
             string json,
             string name,
             string description,
             string hitPolicy,
             string status,
             int version,
             long? userId = null)
        {
            const string sql = @"
                update rulesengine.cfgruledecisiontable
                set
                  ruledecisiontablename = @Name,
                  description = @Description,
                  hitpolicy = @HitPolicy,
                  deploymentstatus = @Status,
                  version = @Version,
                  decisiontablejson = @Json::jsonb,
                  updatedon = now(),
                  updatedby = @UserId
                where uniquedecisiontableid = @Id
                  and deletedon is null;";

            using var db = Conn();
            await db.ExecuteAsync(sql, new
            {
                Id = id,
                Name = name,
                Description = description ?? "",
                HitPolicy = hitPolicy,
                Status = status,
                Version = version,
                Json = json,
                UserId = userId
            });
        }

        public async Task<RuleDto?> GetRealtimeRuleByDecisionTableIdAsync(string decisionTableId)
        {
            const string sql = @"
                select
                  ruleid as Id,
                  rulegroupid as RuleGroupId,
                  rulename as Name,
                  ruletype as RuleType,
                  ruledescription as Description,
                  rulejson::text as RuleJson,
                  activeflag as ActiveFlag,
                  createdon as CreatedOn,
                  createdby as CreatedBy,
                  updatedon as UpdatedOn,
                  updatedby as UpdatedBy,
                  deletedon as DeletedOn,
                  deletedby as DeletedBy
                from rulesengine.cfgrule
                where deletedon is null
                  and activeflag = true
                  and upper(ruletype) = 'REALTIME'
                  and (rulejson->'decisionTable'->>'id') = @DecisionTableId
                order by updatedon desc nulls last, createdon desc
                limit 1;";

            using var db = Conn();
            return await db.QueryFirstOrDefaultAsync<RuleDto>(sql, new { DecisionTableId = decisionTableId });
        }

        public async Task UpdateRuleJsonAsync(long ruleId, string ruleJson, long? userId = null)
        {
            const string sql = @"
                update rulesengine.cfgrule
                set
                  rulejson = @RuleJson::jsonb,
                  updatedon = now(),
                  updatedby = @UserId
                where ruleid = @Id
                  and deletedon is null;";

            using var db = Conn();
            await db.ExecuteAsync(sql, new { Id = ruleId, RuleJson = ruleJson, UserId = userId });
        }


        public async Task SoftDeleteDecisionTableAsync(string id, long? userId = null)
        {
            const string sql = @"
                update rulesengine.cfgruledecisiontable
                set activeflag = false, deletedon = now(), deletedby = @UserId
                where uniquedecisiontableid = @Id
                  and deletedon is null;";

            using var db = Conn();
            await db.ExecuteAsync(sql, new { Id = id, UserId = userId });
        }




        public async Task<IReadOnlyList<RuleDataFieldDto>> GetRuleDataFieldsAsync(long? moduleId = null)
        {
            const string sql = @"
                select
                  d.ruledatafieldid as RuleDataFieldId,
                  d.moduleid as ModuleId,
                  coalesce(m.modulename, '') as ModuleName,
                  coalesce(d.ruledatafieldjson::text, '{}'::text) as RuleDataFieldJson,
                  d.activeflag as ActiveFlag,
                  d.createdon as CreatedOn,
                  d.createdby as CreatedBy,
                  d.updatedon as UpdatedOn,
                  d.updatedby as UpdatedBy,
                  d.deletedon as DeletedOn,
                  d.deletedby as DeletedBy
                from rulesengine.cfgruledatafield d
                left join public.cfgmodule m
                  on m.moduleid = d.moduleid
                where d.deletedon is null
                  and (@ModuleId is null or d.moduleid = @ModuleId)
                order by d.moduleid, d.ruledatafieldid;";

            using var db = Conn();
            var rows = await db.QueryAsync<RuleDataFieldDto>(sql, new { ModuleId = moduleId });
            return rows.AsList();
        }

        public async Task<string?> GetRuleDataFieldJsonAsync(long ruleDataFieldId)
        {
            const string sql = @"
                select d.ruledatafieldjson::text
                from rulesengine.cfgruledatafield d
                where d.ruledatafieldid = @Id
                  and d.deletedon is null;";

            using var db = Conn();
            return await db.ExecuteScalarAsync<string?>(sql, new { Id = ruleDataFieldId });
        }



        public async Task<IReadOnlyList<RuleDataFunctionListDto>> GetRuleDataFunctionsAsync()
        {
            const string sql = @"
        select
          ruledatafunctionid as Id,
          ruledatafunctionname as Name,
          deploymentstatus as DeploymentStatus,
          version,
          coalesce(updatedon, createdon) as UpdatedOn,
          activeflag as ActiveFlag
        from rulesengine.cfgruledatafunction
        where deletedon is null
        order by ruledatafunctionid desc;";

            using var db = Conn();
            var rows = await db.QueryAsync<RuleDataFunctionListDto>(sql);
            return rows.AsList();
        }

        public async Task<RuleDataFunctionDto?> GetRuleDataFunctionAsync(long id)
        {
            const string sql = @"
        select
          ruledatafunctionid as RuleDataFunctionId,
          ruledatafunctionname as RuleDataFunctionName,
          description,
          deploymentstatus as DeploymentStatus,
          version,
          ruledatafunctionjson::text as RuleDataFunctionJson,
          activeflag as ActiveFlag,
          createdon as CreatedOn,
          createdby as CreatedBy,
          updatedon as UpdatedOn,
          updatedby as UpdatedBy,
          deletedon as DeletedOn,
          deletedby as DeletedBy
        from rulesengine.cfgruledatafunction
        where ruledatafunctionid = @Id
          and deletedon is null;";

            using var db = Conn();
            return await db.QueryFirstOrDefaultAsync<RuleDataFunctionDto>(sql, new { Id = id });
        }

        public async Task<string?> GetRuleDataFunctionJsonAsync(long id)
        {
            const string sql = @"
        select ruledatafunctionjson::text
        from rulesengine.cfgruledatafunction
        where ruledatafunctionid = @Id
          and deletedon is null;";

            using var db = Conn();
            return await db.ExecuteScalarAsync<string?>(sql, new { Id = id });
        }

        public async Task<long> CreateRuleDataFunctionAsync(UpsertRuleDataFunctionRequest req, long? userId = null)
        {
            // IMPORTANT:
            // Your DDL shows ruledatafunctionid has no DEFAULT/IDENTITY.
            // If the DB does NOT auto-generate it, change insert to supply an Id (or add a sequence default).
            const string sql = @"
        insert into rulesengine.cfgruledatafunction
          (ruledatafunctionname, description, deploymentstatus, version, ruledatafunctionjson, activeflag, createdby)
        values
          (@Name, @Description, @DeploymentStatus, @Version, @Json::jsonb, @ActiveFlag, @UserId)
        returning ruledatafunctionid;";

            var json = JsonSerializer.Serialize(req.RuleDataFunctionJson);

            using var db = Conn();
            return await db.ExecuteScalarAsync<long>(sql, new
            {
                req.Name,
                req.Description,
                req.DeploymentStatus,
                req.Version,
                Json = json,
                req.ActiveFlag,
                UserId = userId
            });
        }

        public async Task UpdateRuleDataFunctionAsync(long id, UpsertRuleDataFunctionRequest req, long? userId = null)
        {
            const string sql = @"
        update rulesengine.cfgruledatafunction
        set
          ruledatafunctionname = @Name,
          description = @Description,
          deploymentstatus = @DeploymentStatus,
          version = @Version,
          ruledatafunctionjson = @Json::jsonb,
          activeflag = @ActiveFlag,
          updatedon = now(),
          updatedby = @UserId
        where ruledatafunctionid = @Id
          and deletedon is null;";

            var json = JsonSerializer.Serialize(req.RuleDataFunctionJson);

            using var db = Conn();
            await db.ExecuteAsync(sql, new
            {
                Id = id,
                req.Name,
                req.Description,
                req.DeploymentStatus,
                req.Version,
                Json = json,
                req.ActiveFlag,
                UserId = userId
            });
        }

        public async Task SoftDeleteRuleDataFunctionAsync(long id, long? userId = null)
        {
            const string sql = @"
        update rulesengine.cfgruledatafunction
        set activeflag = false,
            deletedon = now(),
            deletedby = @UserId
        where ruledatafunctionid = @Id
          and deletedon is null;";

            using var db = Conn();
            await db.ExecuteAsync(sql, new { Id = id, UserId = userId });
        }




        public async Task<RulesDashboardCountsRow> GetDashboardCountsAsync()
        {
            const string sql = @"
        select
          (select count(*)
             from rulesengine.cfgrule
            where deletedon is null
              and activeflag = true) as ActiveRules,

          (select count(*)
             from rulesengine.cfgrulegroup
            where deletedon is null) as RuleGroupsTotal,

          (select count(*)
             from rulesengine.cfgrulegroup
            where deletedon is null
              and activeflag = true) as RuleGroupsActive,

          (select count(*)
             from rulesengine.cfgruledatafunction
            where deletedon is null) as DataFunctionsTotal,

          (select count(*)
             from rulesengine.cfgruledatafunction
            where deletedon is null
              and activeflag = true) as DataFunctionsActive;
    ";

            using var db = Conn();
            return await db.QuerySingleAsync<RulesDashboardCountsRow>(sql);
        }

    }
}
