using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
public class DashBoardController : ControllerBase
{
    private readonly IDashboardRepository _dashBoardService;

    public DashBoardController(IDashboardRepository dashBoardService)
    {
        _dashBoardService = dashBoardService;
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> DashBoardCount(int userId)
    {
        var result = await _dashBoardService.DashBoardCount(userId);
        if (result == null)
            return NotFound(new { message = "No dashboard data found for this user." });
        return Ok(result);
    }
    [HttpGet("carestaff/{userId}")]
    public async Task<IActionResult> GetMyCareStaff(int userId)
    {
        var result = await _dashBoardService.GetMyCareStaff(userId);
        if (result == null || result.Count == 0)
            return NotFound(new { message = "No care staff data found for this user." });
        return Ok(result);
    }
    [HttpGet("membersummaries/{userId}")]
    public async Task<IActionResult> GetMemberSummaries(int userId)
    {
        var result = await _dashBoardService.GetMemberSummaries(userId);
        if (result == null || result.Count == 0)
            return NotFound(new { message = "No member summaries found for this user." });
        return Ok(result);
    }
    [HttpGet("authdetails/{userId}")]
    public async Task<IActionResult> GetAuthDetailListAsync(int userId)
    {
        var result = await _dashBoardService.GetAuthDetailListAsync(userId);
        if (result == null || result.Count == 0)
            return NotFound(new { message = "No authorization details found for this user." });
        return Ok(result);
    }
    [HttpGet("pendingauthactivities/{userId?}")]
    public async Task<IActionResult> GetPendingAuthActivitiesAsync(int? userId = null)
    {
        var result = await _dashBoardService.GetPendingAuthActivitiesAsync(userId);
        if (result == null || result.Count == 0)
            return NotFound(new { message = "No pending authorization activities found." });
        return Ok(result);
    }
    [HttpGet("pendingwq/{userId?}")]
    public async Task<IActionResult> GetPendingWQAsync(int? userId = null)
    {
        var result = await _dashBoardService.GetPendingWQAsync(userId);
        if (result == null || result.Count == 0)
            return NotFound(new { message = "No pending work queue items found." });
        return Ok(result);
    }
    [HttpGet("wqactivitylines/{activityid?}")]
    public async Task<IActionResult> GetWQActivityLines(int? activityid = null)
    {
        var result = await _dashBoardService.GetWQActivityLines(activityid);
        if (result == null || result.Count == 0)
            return NotFound(new { message = "No work queue activity lines found." });
        return Ok(result);
    }
    [HttpPost("updateactivitylines")]
    public async Task<IActionResult> UpdateAuthActivityLinesAsync([FromBody] UpdateActivityLinesRequest request)
    {
        if (request.LineIds == null || !request.LineIds.Any())
            return BadRequest(new { message = "LineIds cannot be null or empty." });
        var updatedCount = await _dashBoardService.UpdateAuthActivityLinesAsync(request.LineIds, request.Status, request.MDDecision, request.MDNotes, request.ReviewedByUserId);
        return Ok(new { updatedCount });
    }

    public class UpdateActivityLinesRequest
    {
        public List<int> LineIds { get; set; } = new List<int>();
        public string Status { get; set; } = string.Empty;
        public string MDDecision { get; set; } = string.Empty;
        public string MDNotes { get; set; } = string.Empty;
        public int ReviewedByUserId { get; set; }
    }

    [HttpPost("insertfaxfile")]
    public async Task<IActionResult> InsertFaxFileAsync([FromBody] FaxFile fax)
    {
        if (fax == null)
            return BadRequest(new { message = "Fax file data cannot be null." });
        var newId = await _dashBoardService.InsertFaxFileAsync(fax);
        return Ok(new { newId });
    }

    [HttpPost("updatefaxfile")]
    public async Task<IActionResult> UpdateFaxFileAsync([FromBody] FaxFile fax)
    {
        if (fax == null || fax.FaxId == 0)
            return BadRequest(new { message = "Invalid fax file data." });
        var updatedRows = await _dashBoardService.UpdateFaxFileAsync(fax);
        return Ok(new { updatedRows });
    }
    [HttpGet("faxfiles")]
    public async Task<IActionResult> GetFaxFilesAsync([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? status = null)
    {
        if (page <= 0 || pageSize <= 0)
            return BadRequest(new { message = "Page and PageSize must be greater than zero." });
        var (items, total) = await _dashBoardService.GetFaxFilesAsync(search, page, pageSize, status);
        if (items == null || items.Count == 0)
            return NotFound(new { message = "No fax files found." });
        var response = new
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)total / pageSize)
        };
        return Ok(response);
    }
    [HttpGet("faxfile/{faxId}")]
    public async Task<IActionResult> GetFaxFileByIdAsync(long faxId)
    {
        if (faxId <= 0)
            return BadRequest(new { message = "Invalid FaxId." });
        var fax = await _dashBoardService.GetFaxFileByIdAsync(faxId);
        if (fax == null)
            return NotFound(new { message = "Fax file not found." });
        return Ok(fax);
    }
}
