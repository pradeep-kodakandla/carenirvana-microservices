
using CareNirvana.Domain.MemberJourney;
using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using Microsoft.AspNetCore.Mvc;
using static CareNirvana.Service.Domain.Model.MemberJourney;

[Route("api/MemberJourney/{memberDetailsId:long}/journey")]
[ApiController]
public class MemberJourneyController : ControllerBase
{
    private readonly IMemberJourney _MemberJourneyService;

    public MemberJourneyController(IMemberJourney memberJourneyService)
    {
        _MemberJourneyService = memberJourneyService;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
                long memberDetailsId,
                [FromQuery] DateTime? fromUtc,
                [FromQuery] DateTime? toUtc,
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 25,
                [FromQuery] string? search = null,
                [FromQuery] int[]? categories = null // int values of EventCategory
            )
    {
        var req = new MemberJourneyRequest
        {
            MemberDetailsId = memberDetailsId,
            FromUtc = fromUtc,
            ToUtc = toUtc,
            Page = Math.Max(1, page),
            PageSize = Math.Clamp(pageSize, 1, 200),
            Search = search,
            Categories = categories?.Select(c => (EventCategory)c).ToArray()
        };

        var result = await _MemberJourneyService.GetMemberJourneyAsync(req); // correct method name/signature
        return Ok(new { page = result.Page, summary = result.Summary });

    }
}
