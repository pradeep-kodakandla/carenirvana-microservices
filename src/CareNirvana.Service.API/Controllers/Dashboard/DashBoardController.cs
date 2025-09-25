using CareNirvana.Service.Application.Interfaces;
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
}
