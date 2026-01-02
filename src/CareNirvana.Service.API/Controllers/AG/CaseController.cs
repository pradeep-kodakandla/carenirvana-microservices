
using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]

public class CaseController : ControllerBase
{
    private readonly ICaseRepository _caseRepository;

    public CaseController(ICaseRepository caseRepository)
    {
        _caseRepository = caseRepository;
    }

    [HttpGet("{caseNumber}")]
    public async Task<IActionResult> GetCaseByNumber(string caseNumber, [FromQuery] bool includeDeleted = false)
    {
        if (string.IsNullOrWhiteSpace(caseNumber))
            return BadRequest(new { message = "Case number is required." });

        var caseAggregate = await _caseRepository.GetCaseByNumberAsync(caseNumber, includeDeleted);
        if (caseAggregate == null)
            return NotFound(new { message = "Case not found." });

        return Ok(caseAggregate);
    }

    [HttpGet("ByHeader/{caseHeaderId:long}")]
    public async Task<IActionResult> GetCaseByHeaderId(long caseHeaderId, [FromQuery] bool includeDeleted = false)
    {
        if (caseHeaderId <= 0)
            return BadRequest(new { message = "Invalid CaseHeaderId." });
        var caseAggregate = await _caseRepository.GetCaseByHeaderIdAsync(caseHeaderId, includeDeleted);
        if (caseAggregate == null)
            return NotFound(new { message = "Case not found." });
        return Ok(caseAggregate);
    }

    // POST api/case?userId=123
    [HttpPost]
    public async Task<IActionResult> CreateCase([FromBody] CreateCaseRequest req, [FromQuery] long userId)
    {
        if (req == null)
            return BadRequest(new { message = "Invalid case data." });

        if (string.IsNullOrWhiteSpace(req.CaseNumber))
            return BadRequest(new { message = "CaseNumber is required." });

        if (req.LevelId <= 0)
            return BadRequest(new { message = "LevelId is required." });

        // Recommended: repo returns header+detail ids + caseLevelNumber
        CreateCaseResult result = await _caseRepository.CreateCaseAsync(req, userId);

        return CreatedAtAction(
            nameof(GetCaseByNumber),
            new { caseNumber = result.CaseNumber },
            result
        );
    }

    // POST api/case/addlevel?userId=123
    [HttpPost("addlevel")]
    public async Task<IActionResult> AddCaseLevel([FromBody] AddCaseLevelRequest req, [FromQuery] long userId)
    {
        if (req == null)
            return BadRequest(new { message = "Invalid case level data." });

        if (req.CaseHeaderId <= 0)
            return BadRequest(new { message = "CaseHeaderId is required." });

        if (req.LevelId <= 0)
            return BadRequest(new { message = "LevelId is required." });

        AddLevelResult result = await _caseRepository.AddCaseLevelAsync(req, userId);

        return CreatedAtAction(
            nameof(GetCaseByNumber),
            new { caseNumber = result.CaseNumber },
            result
        );
    }

    // PUT api/case/updatedetail?userId=123
    [HttpPut("updatedetail")]
    public async Task<IActionResult> UpdateCaseDetail([FromBody] UpdateCaseDetailRequest req, [FromQuery] long userId)
    {
        Console.WriteLine("UpdateCaseDetail called");
        if (req == null)
            return BadRequest(new { message = "Invalid case detail data." });

        if (req.CaseDetailId <= 0)
            return BadRequest(new { message = "CaseDetailId is required." });
        Console.WriteLine("Updating case detail...", req.CaseDetailId);
        await _caseRepository.UpdateCaseDetailAsync(req, userId);
        return NoContent();
    }

    // DELETE api/case/header/{caseHeaderId}?userId=123&cascadeDetails=true
    [HttpDelete("header/{caseHeaderId:long}")]
    public async Task<IActionResult> SoftDeleteCaseHeader(long caseHeaderId, [FromQuery] long userId, [FromQuery] bool cascadeDetails = true)
    {
        if (caseHeaderId <= 0)
            return BadRequest(new { message = "Invalid CaseHeaderId." });

        await _caseRepository.SoftDeleteCaseHeaderAsync(caseHeaderId, userId, cascadeDetails);
        return NoContent();
    }

    // DELETE api/case/detail/{caseDetailId}?userId=123
    [HttpDelete("detail/{caseDetailId:long}")]
    public async Task<IActionResult> SoftDeleteCaseDetail(long caseDetailId, [FromQuery] long userId)
    {
        if (caseDetailId <= 0)
            return BadRequest(new { message = "Invalid CaseDetailId." });

        await _caseRepository.SoftDeleteCaseDetailAsync(caseDetailId, userId);
        return NoContent();
    }

    [HttpGet("ByMember/{memberDetailId:long}")]
    public async Task<IActionResult> GetCasesByMemberDetailId(
        long memberDetailId,
        [FromQuery] bool includeDetails = false,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] string? statuses = null) // comma separated: Open,Close,Reopen
    {
        var statusList = string.IsNullOrWhiteSpace(statuses)
            ? null
            : statuses.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var cases = await _caseRepository.GetCasesByMemberDetailIdAsync(
            memberDetailId,
            includeDetails,
            statusList,
            includeDeleted);

        return Ok(cases);
    }

    [HttpGet("AgCasesByMember/{memberDetailId:int}")]
    public async Task<IActionResult> GetAgCasesByMemberDetailId(
        int memberDetailId,
        CancellationToken ct = default)
    {
        var cases = await _caseRepository.GetAgCasesByMemberAsync(memberDetailId, ct);
        return Ok(cases);
    }
}



