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
                  name,
                  scheduletype as ScheduleType,
                  description,
                  purpose
                from rulesengine.rulegroup
                where isdeleted = false
                order by rulegroupid desc;";

            using var db = Conn();
            var rows = await db.QueryAsync<RuleGroupDto>(sql);
            return rows.AsList();
        }

        public async Task<long> CreateRuleGroupAsync(UpsertRuleGroupRequest req, long? userId = null)
        {
            const string sql = @"
                insert into rulesengine.rulegroup (name, scheduletype, description, purpose, createdby)
                values (@Name, @ScheduleType, @Description, @Purpose, @UserId)
                returning rulegroupid;";

            using var db = Conn();
            return await db.ExecuteScalarAsync<long>(sql, new
            {
                req.Name,
                req.ScheduleType,
                req.Description,
                req.Purpose,
                UserId = userId
            });
        }

        public async Task UpdateRuleGroupAsync(long id, UpsertRuleGroupRequest req, long? userId = null)
        {
            const string sql = @"
                update rulesengine.rulegroup
                set
                  name = @Name,
                  scheduletype = @ScheduleType,
                  description = @Description,
                  purpose = @Purpose,
                  updatedat = now(),
                  updatedby = @UserId
                where rulegroupid = @Id and isdeleted = false;";

            using var db = Conn();
            await db.ExecuteAsync(sql, new
            {
                Id = id,
                req.Name,
                req.ScheduleType,
                req.Description,
                req.Purpose,
                UserId = userId
            });
        }

        public async Task SoftDeleteRuleGroupAsync(long id, long? userId = null)
        {
            // soft delete group + soft delete its rules
            const string sql = @"
                update rulesengine.rulegroup
                set isdeleted = true, deletedat = now(), deletedby = @UserId
                where rulegroupid = @Id and isdeleted = false;

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
                  ruledefid as Id,
                  rulegroupid as RuleGroupId,
                  name,
                  ruletype as RuleType,
                  description,
                  rulejson::text as RuleJson
                from rulesengine.ruledef
                where isdeleted = false
                  and (@RuleGroupId is null or rulegroupid = @RuleGroupId)
                order by ruledefid desc;";

            using var db = Conn();
            var rows = await db.QueryAsync<RuleDto>(sql, new { RuleGroupId = ruleGroupId });
            return rows.AsList();
        }

        public async Task<long> CreateRuleAsync(UpsertRuleRequest req, long? userId = null)
        {
            const string sql = @"
                insert into rulesengine.ruledef (rulegroupid, name, ruletype, description, rulejson, createdby)
                values (@RuleGroupId, @Name, @RuleType, @Description, @RuleJson::jsonb, @UserId)
                returning ruledefid;";

            using var db = Conn();
            return await db.ExecuteScalarAsync<long>(sql, new
            {
                req.RuleGroupId,
                req.Name,
                req.RuleType,
                req.Description,
                RuleJson = req.RuleJson,
                UserId = userId
            });
        }

        public async Task UpdateRuleAsync(long id, UpsertRuleRequest req, long? userId = null)
        {
            const string sql = @"
                update rulesengine.ruledef
                set
                  rulegroupid = @RuleGroupId,
                  name = @Name,
                  ruletype = @RuleType,
                  description = @Description,
                  rulejson = @RuleJson::jsonb,
                  updatedat = now(),
                  updatedby = @UserId
                where ruledefid = @Id and isdeleted = false;";

            using var db = Conn();
            await db.ExecuteAsync(sql, new
            {
                Id = id,
                req.RuleGroupId,
                req.Name,
                req.RuleType,
                req.Description,
                RuleJson = req.RuleJson,
                UserId = userId
            });
        }

        public async Task SoftDeleteRuleAsync(long id, long? userId = null)
        {
            const string sql = @"
            update rulesengine.ruledef
            set isdeleted = true, deletedat = now(), deletedby = @UserId
            where ruledefid = @Id and isdeleted = false;";

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
                select decisiontabletemplateid as Id, name, status, version, updatedon as UpdatedOn
                from rulesengine.decisiontabletemplate
                where isdeleted = false
                order by lower(name);";

            using var db = Conn();
            var rows = await db.QueryAsync<DecisionTableListDto>(sql);
            return rows.AsList();
        }

        public async Task<string?> GetDecisionTableJsonAsync(string id)
        {
            const string sql = @"
                select templatejson::text
                from rulesengine.decisiontabletemplate
                where decisiontabletemplateid = @Id and isdeleted = false;";

            using var db = Conn();
            return await db.ExecuteScalarAsync<string?>(sql, new { Id = id });
        }

        public async Task<string> CreateDecisionTableAsync(
            string json, string id, string name, string description, string hitPolicy, string status, int version, long? userId = null)
        {
            const string sql = @"
                insert into rulesengine.decisiontabletemplate
                (decisiontabletemplateid, name, description, hitpolicy, status, version, updatedon, templatejson, createdby)
                values
                (@Id, @Name, @Description, @HitPolicy, @Status, @Version, now(), @Json::jsonb, @UserId);";

            using var db = Conn();
            await db.ExecuteAsync(sql, new { Id = id, Name = name, Description = description, HitPolicy = hitPolicy, Status = status, Version = version, Json = json, UserId = userId });
            return id;
        }

        public async Task UpdateDecisionTableAsync(
            string id, string json, string name, string description, string hitPolicy, string status, int version, long? userId = null)
        {
            const string sql = @"
                update rulesengine.decisiontabletemplate
                set
                  name = @Name,
                  description = @Description,
                  hitpolicy = @HitPolicy,
                  status = @Status,
                  version = @Version,
                  updatedon = now(),
                  templatejson = @Json::jsonb,
                  updatedat = now(),
                  updatedby = @UserId
                where decisiontabletemplateid = @Id and isdeleted = false;";

            using var db = Conn();
            await db.ExecuteAsync(sql, new { Id = id, Name = name, Description = description, HitPolicy = hitPolicy, Status = status, Version = version, Json = json, UserId = userId });
        }

        public async Task SoftDeleteDecisionTableAsync(string id, long? userId = null)
        {
            const string sql = @"
                update rulesengine.decisiontabletemplate
                set isdeleted = true, deletedat = now(), deletedby = @UserId
                where decisiontabletemplateid = @Id and isdeleted = false;";

            using var db = Conn();
            await db.ExecuteAsync(sql, new { Id = id, UserId = userId });
        }

    }
}
