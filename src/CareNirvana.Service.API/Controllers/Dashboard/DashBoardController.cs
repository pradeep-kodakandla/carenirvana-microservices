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

}
