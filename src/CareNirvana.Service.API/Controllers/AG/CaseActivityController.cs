using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
public class CaseActivityController : ControllerBase
{
    private readonly ICaseActivityRepository _repo;

    public CaseActivityController(ICaseActivityRepository repo)
    {
        _repo = repo;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CaseActivityRowDto>>> GetByCase(
       [FromQuery] int caseHeaderId,
       [FromQuery] int memberDetailsId,
       [FromQuery] int caseLevelId,
       [FromQuery] string status = "all",
       CancellationToken ct = default)
    {
        var rows = await _repo.GetByCaseAsync(caseHeaderId, memberDetailsId, caseLevelId, status, ct);
        return Ok(rows);
    }

    [HttpGet("{caseActivityId:int}")]
    public async Task<ActionResult<CaseActivityRowDto>> GetById(int caseActivityId, CancellationToken ct = default)
    {
        var row = await _repo.GetByIdAsync(caseActivityId, ct);
        return row is null ? NotFound() : Ok(row);
    }

    [HttpPost]
    public async Task<ActionResult<int>> Insert([FromBody] CaseActivityCreateDto dto, CancellationToken ct = default)
    {
        var id = await _repo.InsertAsync(dto, ct);
        return Ok(id);
    }

    [HttpPut]
    public async Task<ActionResult> Update([FromBody] CaseActivityUpdateDto dto, CancellationToken ct = default)
    {
        var ok = await _repo.UpdateAsync(dto, ct);
        return ok ? Ok() : NotFound();
    }

    [HttpDelete("{caseActivityId:int}")]
    public async Task<ActionResult> Delete(int caseActivityId, [FromQuery] int deletedBy, CancellationToken ct = default)
    {
        var ok = await _repo.DeleteAsync(caseActivityId, deletedBy, ct);
        return ok ? Ok() : NotFound();
    }

    // ✅ Accept group activity: inserts/updates action + updates caseactivity.referto
    [HttpPost("{caseActivityId:int}/accept")]
    public async Task<ActionResult> Accept(int caseActivityId, [FromBody] WorkgroupActionDto dto, CancellationToken ct = default)
    {
        var ok = await _repo.AcceptGroupActivityAsync(caseActivityId, dto, ct);
        return ok ? Ok() : Conflict("Activity already accepted by another user (referto is not null).");
    }

    // ✅ Reject group activity: inserts/updates action; if all users rejected => workgroup row is rejected
    [HttpPost("{caseActivityId:int}/reject")]
    public async Task<ActionResult> Reject(int caseActivityId, [FromBody] WorkgroupActionDto dto, CancellationToken ct = default)
    {
        var ok = await _repo.RejectGroupActivityAsync(caseActivityId, dto, ct);
        return ok ? Ok() : NotFound();
    }

    // ✅ “still in request status” for a user (pending actions)
    [HttpGet("workgroup/pending")]
    public async Task<ActionResult<IReadOnlyList<CaseActivityRowDto>>> GetPendingForUser(
        [FromQuery] int userId,
        [FromQuery] int caseHeaderId,
        [FromQuery] int memberDetailsId,
        [FromQuery] int caseLevelId,
        CancellationToken ct = default)
    {
        var rows = await _repo.GetPendingRequestsForUserAsync(userId, caseHeaderId, memberDetailsId, caseLevelId, ct);
        return Ok(rows);
    }

    // ✅ Accepted for a user
    [HttpGet("workgroup/accepted")]
    public async Task<ActionResult<IReadOnlyList<CaseActivityRowDto>>> GetAcceptedForUser(
        [FromQuery] int userId,
        [FromQuery] int caseHeaderId,
        [FromQuery] int memberDetailsId,
        [FromQuery] int caseLevelId,
        CancellationToken ct = default)
    {
        var rows = await _repo.GetAcceptedForUserAsync(userId, caseHeaderId, memberDetailsId, caseLevelId, ct);
        return Ok(rows);
    }

    [HttpGet("template/{caseTemplateId:int}")]
    public async Task<ActionResult<CaseActivityTemplateResponse>> GetCaseActivityTemplate(int caseTemplateId, CancellationToken ct = default)
    {
        var template = await _repo.GetCaseActivityTemplateAsync(caseTemplateId, ct);
        return template is null ? NotFound() : Ok(template);
    }
}


