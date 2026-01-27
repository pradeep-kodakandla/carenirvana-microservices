using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Text.Json;
using System.Globalization;

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
                    hitpolicy as HitPolicy,         
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



        public async Task<IReadOnlyList<TriggerRuleRow>> GetActiveRulesForTriggerAsync(string triggerKey)
        {
            const string sql = @"
                  select
                    t.triggerid as TriggerId,
                    t.triggerkey as TriggerKey,
                    t.moduleid as ModuleId,

                    r.ruleid as RuleId,
                    r.rulename as RuleName,
                    r.ruletype as RuleType,
                    r.rulejson::text as RuleJson,

                    m.sequence as Sequence,
                    m.stop_on_match as StopOnMatch
                  from rulesengine.cfgruletrigger t
                  join rulesengine.cfgruletriggermap m
                    on m.triggerid = t.triggerid
                   and m.activeflag = true
                  join rulesengine.cfgrule r
                    on r.ruleid = m.ruleid
                   and r.activeflag = true
                   and r.deletedon is null
                  where t.triggerkey = @TriggerKey
                    and t.activeflag = true
                    and t.deletedon is null
                  order by m.sequence asc;";

            using var db = Conn();
            var rows = await db.QueryAsync<TriggerRuleRow>(sql, new { TriggerKey = triggerKey });
            return rows.AsList();
        }

        public async Task InsertRuleExecutionLogAsync(object logRow)
        {
            const string sql = @"
              insert into rulesengine.cfgruleexecutionlog
              (
                correlationid, triggerid, triggerkey, moduleid,
                requesteduserid, clientapp, clientip, useragent,
                authid, memberid, patientid, servicerequestid,
                requestjson, responsejson,
                status, matchedruleid, matchedrulename, evaluatedruleids,
                receivedon, responsetime_ms, errormessage
              )
              values
              (
                @CorrelationId, @TriggerId, @TriggerKey, @ModuleId,
                @RequestedUserId, @ClientApp, @ClientIp, @UserAgent,
                @AuthId, @MemberId, @PatientId, @ServiceRequestId,
                @RequestJson::jsonb, @ResponseJson::jsonb,
                @Status, @MatchedRuleId, @MatchedRuleName, @EvaluatedRuleIds,
                now(), @ResponseTimeMs, @ErrorMessage
              );";

            using var db = Conn();
            await db.ExecuteAsync(sql, logRow);
        }

        public static class DecisionTableEvaluator
        {
            public static (bool Matched, Dictionary<string, string?> Outputs) Evaluate(string ruleJson, JsonElement facts)
            {
                using var doc = JsonDocument.Parse(ruleJson);
                var root = doc.RootElement;

                // Expecting root.engine.rules[] like your JSON
                if (!root.TryGetProperty("engine", out var eng) ||
                    !eng.TryGetProperty("rules", out var rulesElem) ||
                    rulesElem.ValueKind != JsonValueKind.Array)
                {
                    return (false, new Dictionary<string, string?>());
                }

                // hitPolicy FIRST: iterate by priority asc (fallback to large number)
                var rules = rulesElem.EnumerateArray()
                    .Where(r => r.TryGetProperty("enabled", out var en) && en.ValueKind == JsonValueKind.True)
                    .OrderBy(r => r.TryGetProperty("priority", out var p) && p.TryGetInt32(out var pv) ? pv : int.MaxValue)
                    .ToList();

                foreach (var r in rules)
                {
                    if (!r.TryGetProperty("when", out var when) ||
                        !when.TryGetProperty("all", out var all) ||
                        all.ValueKind != JsonValueKind.Array)
                        continue;

                    bool ok = true;

                    foreach (var c in all.EnumerateArray())
                    {
                        var fieldPath = c.GetProperty("fieldPath").GetString() ?? "";
                        var op = c.GetProperty("operator").GetString() ?? "";
                        var expected = c.GetProperty("value").GetString() ?? "";

                        var actualElem = TryGetByPath(facts, fieldPath);
                        var actual = actualElem.HasValue ? actualElem.Value.ToString() : "";

                        var opRaw = c.GetProperty("operator").GetString() ?? "";

                        var dataType = c.TryGetProperty("dataType", out var dtEl) ? dtEl.GetString() : null;


                        if (!Compare(actual, expected, opRaw, dataType))
                        {
                            ok = false;
                            break;
                        }
                    }

                    if (ok)
                    {
                        if (r.TryGetProperty("then", out var thenObj) && thenObj.ValueKind == JsonValueKind.Object)
                        {
                            var outputs = new Dictionary<string, string?>();
                            foreach (var prop in thenObj.EnumerateObject())
                                outputs[prop.Name] = prop.Value.ValueKind == JsonValueKind.Null ? null : prop.Value.ToString();

                            return (true, outputs);
                        }

                        return (true, new Dictionary<string, string?>());
                    }
                }

                return (false, new Dictionary<string, string?>());
            }

            private static bool Compare(string actual, string expected, string opRaw, string? dataType)
            {
                var op = (opRaw ?? "").Trim().ToUpperInvariant();

                // normalize common variants from UI
                if (op == "=" || op == "==") op = "EQ";
                if (op == "!=" || op == "<>") op = "NEQ";

                // equality
                if (op == "EQ") return string.Equals(actual, expected, StringComparison.Ordinal);
                if (op == "NEQ") return !string.Equals(actual, expected, StringComparison.Ordinal);

                // try DATE compare (works even if dataType is "string")
                if (TryParseDate(actual, out var ad) && TryParseDate(expected, out var ed))
                    return CompareI(ad, ed, op);

                // try NUMERIC compare
                if (decimal.TryParse(actual, NumberStyles.Any, CultureInfo.InvariantCulture, out var an) &&
                    decimal.TryParse(expected, NumberStyles.Any, CultureInfo.InvariantCulture, out var en))
                    return CompareI(an, en, op);

                // fallback: ordinal string compare for >,<,>=,<=
                var cmp = string.CompareOrdinal(actual, expected);
                return op switch
                {
                    "GT" or ">" => cmp > 0,
                    "GTE" or ">=" => cmp >= 0,
                    "LT" or "<" => cmp < 0,
                    "LTE" or "<=" => cmp <= 0,
                    _ => false
                };
            }

            private static bool CompareI<T>(T a, T e, string op) where T : IComparable<T>
            {
                var cmp = a.CompareTo(e);
                return op switch
                {
                    "GT" or ">" => cmp > 0,
                    "GTE" or ">=" => cmp >= 0,
                    "LT" or "<" => cmp < 0,
                    "LTE" or "<=" => cmp <= 0,
                    _ => false
                };
            }

            private static bool TryParseDate(string s, out DateTime dt)
            {
                dt = default;
                if (string.IsNullOrWhiteSpace(s)) return false;

                // common formats your rules use: 1/1/2026
                var formats = new[]
                {
        "M/d/yyyy", "MM/dd/yyyy",
        "M/d/yy",   "MM/dd/yy",
        "yyyy-MM-dd",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-ddTHH:mm:ssZ",
        "yyyy-MM-ddTHH:mm:ss.fff",
        "yyyy-MM-ddTHH:mm:ss.fffZ"
    };

                if (DateTime.TryParseExact(s.Trim(), formats, CultureInfo.InvariantCulture,
                        DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                        out dt))
                    return true;

                // last resort
                return DateTime.TryParse(s.Trim(), CultureInfo.InvariantCulture,
                    DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out dt);
            }
            private static JsonElement? TryGetByPath(JsonElement root, string path)
            {
                var cur = root;
                foreach (var part in path.Split('.', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (cur.ValueKind != JsonValueKind.Object) return null;
                    if (!cur.TryGetProperty(part, out var next)) return null;
                    cur = next;
                }
                return cur;
            }
        }


        /// </summary>
        public static async Task<(bool Matched, Dictionary<string, string?> Outputs)> EvaluateAsync(
            string ruleJson,
            JsonElement facts,
            Func<string, Task<string?>> loadDecisionTableJsonByIdAsync,
            IDictionary<string, string?>? cache = null)
        {
            // 1) Already compiled ruledoc (engine.rules)
            if (LooksExecutableRuleDoc(ruleJson))
                return DecisionTableEvaluator.Evaluate(ruleJson, facts);

            // 2) Resolve pointer rule -> decision table JSON by id
            var dtId = TryGetDecisionTableId(ruleJson);
            if (string.IsNullOrWhiteSpace(dtId))
                return (false, new Dictionary<string, string?>());

            string? resolvedJson = null;

            if (cache != null &&
                cache.TryGetValue(dtId, out var cached) &&
                !string.IsNullOrWhiteSpace(cached))
            {
                resolvedJson = cached;
            }
            else
            {
                resolvedJson = await loadDecisionTableJsonByIdAsync(dtId);
                if (cache != null) cache[dtId] = resolvedJson;
            }

            if (string.IsNullOrWhiteSpace(resolvedJson))
                return (false, new Dictionary<string, string?>());

            // 3) If resolved DT is compiled ruledoc -> use your existing evaluator
            if (LooksExecutableRuleDoc(resolvedJson))
                return DecisionTableEvaluator.Evaluate(resolvedJson, facts);

            // 4) If resolved DT is RulesDesigner format (rows/columns) -> evaluate directly
            if (LooksDesignerDecisionTable(resolvedJson))
                return EvaluateDesignerDecisionTable(resolvedJson, facts);

            // Unknown shape
            return (false, new Dictionary<string, string?>());
        }

        private static (bool Matched, Dictionary<string, string?> Outputs) EvaluateDesignerDecisionTable(
    string dtJson,
    JsonElement facts)
        {
            using var doc = JsonDocument.Parse(dtJson);
            var root = doc.RootElement;

            var columns = root.GetProperty("columns");
            var rows = root.GetProperty("rows");

            // Gather enabled condition/result columns
            var conditionCols = new List<JsonElement>();
            var resultCols = new List<JsonElement>();

            foreach (var c in columns.EnumerateArray())
            {
                var enabled = c.TryGetProperty("isEnabled", out var en) && en.ValueKind == JsonValueKind.True;
                if (!enabled) continue;

                var kind = c.TryGetProperty("kind", out var k) ? k.GetString() : null;
                if (string.Equals(kind, "condition", StringComparison.OrdinalIgnoreCase))
                    conditionCols.Add(c);
                else if (string.Equals(kind, "result", StringComparison.OrdinalIgnoreCase))
                    resultCols.Add(c);
            }

            // FIRST hit policy: evaluate rows in table order
            foreach (var row in rows.EnumerateArray())
            {
                var rowEnabled = row.TryGetProperty("enabled", out var re) && re.ValueKind == JsonValueKind.True;
                if (!rowEnabled) continue;

                if (!row.TryGetProperty("cells", out var cells) || cells.ValueKind != JsonValueKind.Object)
                    continue;

                bool ok = true;

                // Check all conditions
                foreach (var col in conditionCols)
                {
                    var colId = col.GetProperty("id").GetString() ?? "";
                    var expected = GetCellString(cells, colId);

                    // Blank cell = wildcard
                    if (string.IsNullOrWhiteSpace(expected))
                        continue;

                    var actual = GetFactValueForColumn(facts, col);

                    if (!string.Equals(actual ?? "", expected, StringComparison.Ordinal))
                    {
                        ok = false;
                        break;
                    }
                }

                if (!ok) continue;

                // Build outputs from result columns
                var outputs = new Dictionary<string, string?>(StringComparer.Ordinal);
                foreach (var col in resultCols)
                {
                    var colId = col.GetProperty("id").GetString() ?? "";
                    var outKey = col.TryGetProperty("key", out var keyEl) ? keyEl.GetString() : null;
                    if (string.IsNullOrWhiteSpace(outKey)) continue;

                    outputs[outKey!] = GetCellString(cells, colId);
                }

                return (true, outputs);
            }

            return (false, new Dictionary<string, string?>());
        }

        private static string? GetCellString(JsonElement cellsObj, string colId)
        {
            if (cellsObj.ValueKind != JsonValueKind.Object) return null;
            if (!cellsObj.TryGetProperty(colId, out var v)) return null;
            return v.ValueKind == JsonValueKind.Null ? null : v.ToString();
        }

        /// <summary>
        /// Builds a fact path from mappedFieldPath + label.
        /// Example:
        ///   mappedFieldPath = "memberDetails", label="Member Program" -> memberDetails.memberProgram
        ///   mappedFieldPath = "authClass", label="Auth Class" -> authClass
        ///   mappedFieldPath missing, label="Anchor Source" -> anchorSource
        /// </summary>
        private static string? GetFactValueForColumn(JsonElement facts, JsonElement col)
        {
            var basePath = col.TryGetProperty("mappedFieldPath", out var mp) ? mp.GetString() : null;
            var label = col.TryGetProperty("label", out var l) ? l.GetString() : null;

            var leaf = ToCamelCase(label ?? "");
            if (string.IsNullOrWhiteSpace(leaf))
                leaf = col.TryGetProperty("key", out var k) ? k.GetString() : null;

            var candidates = new List<string>();

            if (!string.IsNullOrWhiteSpace(basePath))
            {
                // if base already looks like a leaf (authClass/authType), keep it
                if (string.Equals(basePath, leaf, StringComparison.OrdinalIgnoreCase) || basePath!.Contains('.'))
                    candidates.Add(basePath!);
                else
                    candidates.Add($"{basePath}.{leaf}");

                // fallback: base alone
                candidates.Add(basePath!);
            }

            if (!string.IsNullOrWhiteSpace(leaf))
                candidates.Add(leaf!);

            foreach (var path in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var elem = TryGetByPath(facts, path);
                if (elem is null) continue;

                // If object, try to pick a useful scalar
                if (elem.Value.ValueKind == JsonValueKind.Object)
                {
                    // prefer leaf prop if present
                    if (!string.IsNullOrWhiteSpace(leaf) &&
                        elem.Value.TryGetProperty(leaf!, out var leafVal) &&
                        leafVal.ValueKind != JsonValueKind.Object &&
                        leafVal.ValueKind != JsonValueKind.Array)
                        return leafVal.ToString();

                    // if single-property object, use its value
                    var props = elem.Value.EnumerateObject().ToList();
                    if (props.Count == 1)
                        return props[0].Value.ToString();

                    // otherwise: object -> string (unlikely match)
                    return elem.Value.ToString();
                }

                return elem.Value.ToString();
            }

            return null;
        }

        private static string ToCamelCase(string label)
        {
            if (string.IsNullOrWhiteSpace(label)) return "";

            var parts = label
                .Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => p.Length > 0)
                .ToList();

            if (parts.Count == 0) return "";

            var first = parts[0].ToLowerInvariant();
            for (int i = 1; i < parts.Count; i++)
            {
                var p = parts[i].ToLowerInvariant();
                parts[i] = char.ToUpperInvariant(p[0]) + p.Substring(1);
            }

            return first + string.Concat(parts.Skip(1));
        }

        private static JsonElement? TryGetByPath(JsonElement root, string path)
        {
            var cur = root;
            foreach (var part in path.Split('.', StringSplitOptions.RemoveEmptyEntries))
            {
                if (cur.ValueKind != JsonValueKind.Object) return null;
                if (!cur.TryGetProperty(part, out var next)) return null;
                cur = next;
            }
            return cur;
        }


        private static bool LooksExecutableRuleDoc(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                return root.TryGetProperty("engine", out var eng) &&
                       eng.TryGetProperty("rules", out var rulesElem) &&
                       rulesElem.ValueKind == JsonValueKind.Array;
            }
            catch { return false; }
        }

        private static bool LooksDesignerDecisionTable(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                return root.TryGetProperty("rows", out var rows) && rows.ValueKind == JsonValueKind.Array &&
                       root.TryGetProperty("columns", out var cols) && cols.ValueKind == JsonValueKind.Array;
            }
            catch { return false; }
        }



        private static string? GetString(JsonElement root, params string[] path)
        {
            var cur = root;
            foreach (var p in path)
            {
                if (cur.ValueKind != JsonValueKind.Object) return null;
                if (!cur.TryGetProperty(p, out var next)) return null;
                cur = next;
            }
            if (cur.ValueKind == JsonValueKind.String) return cur.GetString();
            return cur.ValueKind == JsonValueKind.Null ? null : cur.ToString();
        }

        private static bool LooksExecutable(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                return root.TryGetProperty("engine", out var eng) &&
                       eng.TryGetProperty("rules", out var rulesElem) &&
                       rulesElem.ValueKind == JsonValueKind.Array;
            }
            catch
            {
                return false;
            }
        }

        private static string? TryGetDecisionTableId(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("ui", out var ui) &&
                    ui.TryGetProperty("decisionTable", out var dt) &&
                    dt.TryGetProperty("id", out var idElem) &&
                    idElem.ValueKind == JsonValueKind.String)
                {
                    return idElem.GetString();
                }

                return null;
            }
            catch
            {
                return null;
            }
        }



        public async Task<IReadOnlyList<RuleActionDto>> GetRuleActionsAsync(bool? activeOnly = null)
        {
            const string sql = @"
        select
          ruleactionid as Id,
          ruleactionname as Name,
          ruleactiondescription as Description,
          actionjson::text as ActionJson,
          activeflag as ActiveFlag,
          createdon as CreatedOn,
          createdby as CreatedBy,
          updatedon as UpdatedOn,
          updatedby as UpdatedBy,
          deletedon as DeletedOn,
          deletedby as DeletedBy
        from rulesengine.cfgruleaction
        where deletedon is null
          and (@ActiveOnly is null or activeflag = @ActiveOnly)
        order by ruleactionid desc;";

            using var db = Conn();
            var rows = await db.QueryAsync<RuleActionDto>(sql, new { ActiveOnly = activeOnly });
            return rows.AsList();
        }

        public async Task<RuleActionDto?> GetRuleActionAsync(long id)
        {
            const string sql = @"
        select
          ruleactionid as Id,
          ruleactionname as Name,
          ruleactiondescription as Description,
          actionjson::text as ActionJson,
          activeflag as ActiveFlag,
          createdon as CreatedOn,
          createdby as CreatedBy,
          updatedon as UpdatedOn,
          updatedby as UpdatedBy,
          deletedon as DeletedOn,
          deletedby as DeletedBy
        from rulesengine.cfgruleaction
        where ruleactionid = @Id
          and deletedon is null;";

            using var db = Conn();
            return await db.QueryFirstOrDefaultAsync<RuleActionDto>(sql, new { Id = id });
        }


    }
}
