using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

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

        [HttpDelete("decisiontables/{id}")]
        public async Task<IActionResult> DeleteDecisionTable(string id)
        {
            await _repo.SoftDeleteDecisionTableAsync(id);
            return NoContent();
        }


    }
}