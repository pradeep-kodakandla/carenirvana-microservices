using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using CareNirvana.Service.Infrastructure.Repository;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.Json;
using static CareNirvana.Service.Infrastructure.Repository.RulesEngineRepository;

namespace CareNirvana.Service.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RulesEngineController : ControllerBase
    {
        private readonly IRulesEngineRepository _repo;
        public RulesEngineController(IRulesEngineRepository repo) => _repo = repo;

        [HttpGet("rulegroups")]
        public async Task<ActionResult<IReadOnlyList<RuleGroupDto>>> GetRuleGroups()
            => Ok(await _repo.GetRuleGroupsAsync());

        [HttpPost("rulegroups")]
        public async Task<ActionResult<long>> CreateRuleGroup([FromBody] UpsertRuleGroupRequest req)
            => Ok(await _repo.CreateRuleGroupAsync(req));

        [HttpPut("rulegroups/{id:long}")]
        public async Task<IActionResult> UpdateRuleGroup(long id, [FromBody] UpsertRuleGroupRequest req)
        {
            await _repo.UpdateRuleGroupAsync(id, req);
            return NoContent();
        }

        [HttpDelete("rulegroups/{id:long}")]
        public async Task<IActionResult> DeleteRuleGroup(long id)
        {
            await _repo.SoftDeleteRuleGroupAsync(id);
            return NoContent();
        }

        [HttpGet("rules")]
        public async Task<ActionResult<IReadOnlyList<RuleDto>>> GetRules([FromQuery] long? ruleGroupId = null)
            => Ok(await _repo.GetRulesAsync(ruleGroupId));

        [HttpPost("rules")]
        public async Task<ActionResult<long>> CreateRule([FromBody] UpsertRuleRequest req)
            => Ok(await _repo.CreateRuleAsync(req));

        [HttpPut("rules/{id:long}")]
        public async Task<IActionResult> UpdateRule(long id, [FromBody] UpsertRuleRequest req)
        {
            await _repo.UpdateRuleAsync(id, req);
            return NoContent();
        }

        [HttpDelete("rules/{id:long}")]
        public async Task<IActionResult> DeleteRule(long id)
        {
            await _repo.SoftDeleteRuleAsync(id);
            return NoContent();
        }


        [HttpGet("decisiontables")]
        public async Task<ActionResult<IReadOnlyList<DecisionTableListDto>>> ListDecisionTables()
     => Ok(await _repo.GetDecisionTablesAsync());

        [HttpGet("decisiontables/{id}")]
        public async Task<ActionResult<string>> GetDecisionTable(string id)
        {
            var json = await _repo.GetDecisionTableJsonAsync(id);
            return json == null ? NotFound() : Content(json, "application/json");
        }

        [HttpPost("decisiontables")]
        public async Task<ActionResult<string>> CreateDecisionTable([FromBody] JsonElement body)
        {
            var id = body.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
            if (string.IsNullOrWhiteSpace(id)) id = $"dt-{Guid.NewGuid():N}";

            var name = body.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
            var desc = body.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "";
            var hp = body.TryGetProperty("hitPolicy", out var h) ? h.GetString() ?? "FIRST" : "FIRST";
            var st = body.TryGetProperty("status", out var s) ? s.GetString() ?? "DRAFT" : "DRAFT";
            var ver = body.TryGetProperty("version", out var v) ? v.GetInt32() : 1;

            var json = body.GetRawText();
            await _repo.CreateDecisionTableAsync(json, id!, name, desc, hp, st, ver);
            return Ok(id);
        }

        [HttpPut("decisiontables/{id}")]
        public async Task<IActionResult> UpdateDecisionTable(string id, [FromBody] JsonElement body)
        {
            var name = body.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
            var desc = body.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "";
            var hp = body.TryGetProperty("hitPolicy", out var h) ? h.GetString() ?? "FIRST" : "FIRST";
            var st = body.TryGetProperty("status", out var s) ? s.GetString() ?? "DRAFT" : "DRAFT";
            var ver = body.TryGetProperty("version", out var v) ? v.GetInt32() : 1;

            await _repo.UpdateDecisionTableAsync(id, body.GetRawText(), name, desc, hp, st, ver);
            return NoContent();
        }

        // 1) check if rule exists for table
        [HttpGet("decisiontables/{id}/rule")]
        public async Task<IActionResult> GetRuleForDecisionTable(string id)
        {
            var rule = await _repo.GetRealtimeRuleByDecisionTableIdAsync(id);
            if (rule == null) return NotFound();
            return Ok(new { ruleId = rule.Id, name = rule.Name, ruleGroupId = rule.RuleGroupId, ruleType = rule.RuleType });
        }

        // 2) update rule json from decision table (called after saving table)
        [HttpPut("decisiontables/{id}/rule/{ruleId:long}")]
        public async Task<IActionResult> UpdateRuleForDecisionTable(string id, long ruleId, [FromBody] object ruleJson)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(ruleJson);
            await _repo.UpdateRuleJsonAsync(ruleId, json);
            return Ok();
        }


        [HttpDelete("decisiontables/{id}")]
        public async Task<IActionResult> DeleteDecisionTable(string id)
        {
            await _repo.SoftDeleteDecisionTableAsync(id);
            return NoContent();
        }


        [HttpGet("datafields")]
        public async Task<IActionResult> GetRuleDataFields([FromQuery] long? moduleId = null)
        {
            var rows = await _repo.GetRuleDataFieldsAsync(moduleId);
            return Ok(rows);
        }

        [HttpGet("datafields/{id:long}")]
        public async Task<IActionResult> GetRuleDataFieldJson(long id)
        {
            var json = await _repo.GetRuleDataFieldJsonAsync(id);
            if (json == null) return NotFound();
            return Content(json, "application/json");
        }





        // GET api/rulesengine/ruledatafunctions
        [HttpGet("datafunctions")]
        public async Task<ActionResult<IReadOnlyList<RuleDataFunctionListDto>>> GetRuleDataFunctions()
      => Ok(await _repo.GetRuleDataFunctionsAsync());

        [HttpGet("datafunctions/{id:long}")]
        public async Task<ActionResult<RuleDataFunctionDto>> GetRuleDataFunction(long id)
        {
            var row = await _repo.GetRuleDataFunctionAsync(id);
            return row == null ? NotFound() : Ok(row);
        }

        [HttpGet("datafunctions/{id:long}/json")]
        public async Task<IActionResult> GetRuleDataFunctionJson(long id)
        {
            var json = await _repo.GetRuleDataFunctionJsonAsync(id);
            return json == null ? NotFound() : Content(json, "application/json");
        }

        [HttpPost("datafunctions")]
        public async Task<ActionResult<long>> CreateRuleDataFunction([FromBody] UpsertRuleDataFunctionRequest req)
        {
            var id = await _repo.CreateRuleDataFunctionAsync(req, userId: null);
            return Ok(id);
        }

        [HttpPut("datafunctions/{id:long}")]
        public async Task<IActionResult> UpdateRuleDataFunction(long id, [FromBody] UpsertRuleDataFunctionRequest req)
        {
            await _repo.UpdateRuleDataFunctionAsync(id, req, userId: null);
            return NoContent();
        }

        [HttpDelete("datafunctions/{id:long}")]
        public async Task<IActionResult> DeleteRuleDataFunction(long id)
        {
            await _repo.SoftDeleteRuleDataFunctionAsync(id, userId: null);
            return NoContent();
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult<RulesDashboardStatsDto>> GetDashboard()
        {
            var c = await _repo.GetDashboardCountsAsync();

            // RecordsProcessed: keep static until we wire to execution/audit logs
            var dto = new RulesDashboardStatsDto
            {
                ActiveRules = new DashboardKpiDto
                {
                    Value = c.ActiveRules,
                    Sub = "ActiveFlag = true"
                },
                RuleGroups = new DashboardKpiDto
                {
                    Value = c.RuleGroupsTotal,
                    Sub = $"{c.RuleGroupsActive} active"
                },
                DataFunctions = new DashboardKpiDto
                {
                    Value = c.DataFunctionsTotal,
                    Sub = $"{c.DataFunctionsActive} active"
                },
                RecordsProcessed = new DashboardKpiDto
                {
                    Value = 0,
                    Sub = "Static for now"
                }
            };

            return Ok(dto);
        }

        [HttpPost("executetrigger")]
        public async Task<ActionResult<ExecuteTriggerResponse>> ExecuteTrigger([FromBody] ExecuteTriggerRequest req)
        {
            var correlationId = Guid.NewGuid();
            var sw = Stopwatch.StartNew();

            string status = "NO_MATCH";
            bool matched = false;
            long? matchedRuleId = null;
            string? matchedRuleName = null;
            var outputs = new Dictionary<string, string?>();

            long? triggerId = null;
            int? moduleId = req.ModuleId;
            var evaluatedRuleIds = new List<long>();

            string requestJson = JsonSerializer.Serialize(req);
            string? responseJson = null;
            string? errorMessage = null;

            // cache decisionTableId -> decisionTableJson for this request (avoid repeated DB hits)
            var dtCache = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var mapped = await _repo.GetActiveRulesForTriggerAsync(req.TriggerKey);
                if (mapped.Count == 0)
                {
                    status = "NO_MATCH";
                }
                else
                {
                    triggerId = mapped[0].TriggerId;
                    moduleId ??= mapped[0].ModuleId;

                    foreach (var row in mapped)
                    {
                        evaluatedRuleIds.Add(row.RuleId);

                        // IMPORTANT: supports both "full DT JSON" and "pointer DT JSON"
                        var (m, o) = await RulesEngineRepository.EvaluateAsync(
                            row.RuleJson,
                            req.Facts,
                            _repo.GetDecisionTableJsonAsync,
                            dtCache
                        );

                        if (m)
                        {
                            matched = true;
                            matchedRuleId = row.RuleId;
                            matchedRuleName = row.RuleName;
                            outputs = o;
                            status = "SUCCESS";

                            if (row.StopOnMatch) break;
                        }
                    }
                }

                var resp = new ExecuteTriggerResponse(
                    correlationId,
                    req.TriggerKey,
                    status,
                    matched,
                    matchedRuleId,
                    matchedRuleName,
                    outputs
                );

                responseJson = JsonSerializer.Serialize(resp);
                return Ok(resp);
            }
            catch (Exception ex)
            {
                status = "ERROR";
                errorMessage = ex.Message;

                var resp = new ExecuteTriggerResponse(
                    correlationId,
                    req.TriggerKey,
                    status,
                    false,
                    null,
                    null,
                    new Dictionary<string, string?>()
                );

                responseJson = JsonSerializer.Serialize(resp);
                return BadRequest(resp);
            }
            finally
            {
                sw.Stop();

                try
                {
                    await _repo.InsertRuleExecutionLogAsync(new
                    {
                        CorrelationId = correlationId,
                        TriggerId = triggerId,
                        TriggerKey = req.TriggerKey,
                        ModuleId = moduleId,
                        RequestedUserId = req.RequestedUserId,
                        ClientApp = req.ClientApp,
                        ClientIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
                        UserAgent = Request.Headers.UserAgent.ToString(),

                        AuthId = req.AuthId,
                        MemberId = req.MemberId,
                        PatientId = req.PatientId,
                        ServiceRequestId = req.ServiceRequestId,

                        RequestJson = requestJson,
                        ResponseJson = responseJson ?? "{}",
                        Status = status,
                        MatchedRuleId = matchedRuleId,
                        MatchedRuleName = matchedRuleName,
                        EvaluatedRuleIds = evaluatedRuleIds.ToArray(),
                        ResponseTimeMs = (int)sw.ElapsedMilliseconds,
                        ErrorMessage = errorMessage
                    });
                }
                catch (Exception logEx)
                {
                    Debug.WriteLine("Rule execution log failed: " + logEx);
                }
            }
        }


        // GET api/rulesengine/ruleactions?activeOnly=true|false
        [HttpGet("ruleactions")]
        public async Task<ActionResult<IReadOnlyList<RuleActionDto>>> GetRuleActions([FromQuery] bool? activeOnly = null)
            => Ok(await _repo.GetRuleActionsAsync(activeOnly));

        // GET api/rulesengine/ruleactions/{id}
        [HttpGet("ruleactions/{id:long}")]
        public async Task<ActionResult<RuleActionDto>> GetRuleAction(long id)
        {
            var row = await _repo.GetRuleActionAsync(id);
            return row == null ? NotFound() : Ok(row);
        }


        [HttpGet("executionlogs")]
        public async Task<ActionResult<RulePagedResult<RuleExecutionLogListItemDto>>> GetExecutionLogs(
                [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var res = await _repo.GetRuleExecutionLogsAsync(page, pageSize);
            return Ok(res);
        }

    }
}