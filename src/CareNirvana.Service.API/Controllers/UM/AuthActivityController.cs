using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Application.UseCases;
using CareNirvana.Service.Domain.Model;
using Microsoft.AspNetCore.Mvc;


namespace CareNirvana.Service.API.Controllers
{
    [Route("api/[controller]")]
    public class AuthActivityController : ControllerBase
    {
        private readonly IAuthActivityRepository _service;

        public AuthActivityController(IAuthActivityRepository service)
        {
            _service = service;
        }

        [HttpGet("authdetail/{authdetailid}")]
        public async Task<IActionResult> GetAll(int authdetailid) =>
            Ok(await _service.GetAllAsync(authdetailid));

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var result = await _service.GetByIdAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AuthActivity activity)
        {
            var result = await _service.InsertAsync(activity);
            return CreatedAtAction(nameof(Get), new { id = result.AuthActivityId }, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] AuthActivity activity)
        {
            if (id != activity.AuthActivityId) return BadRequest();
            var result = await _service.UpdateAsync(activity);
            return Ok(result);
        }


        // GET api/AuthActivity/mdreview?activityId=123&authDetailId=456
        [HttpGet("mdreview")]
        public async Task<IActionResult> GetMdReviewActivities( [FromQuery] int? activityId = null, [FromQuery] int? authDetailId = null)
        {
            var list = await _service.GetMdReviewActivitiesAsync(activityId, authDetailId);

            // Turn (Activity, Lines) tuples into objects with properties
            var dto = list.Select(t => new
            {
                Activity = t.Activity,
                Lines = t.Lines
            });

            return Ok(dto);
        }


        // POST api/AuthActivity/mdreview
        [HttpPost("mdreview")]
        public async Task<IActionResult> CreateMdReview([FromBody] MdReviewActivityCreate payload)
        {
            if (payload is null || payload.Activity is null)
                return BadRequest("Payload or Activity cannot be null.");

            var newId = await _service.CreateMdReviewActivityAsync(payload);
            // Reuse the GET above to shape the response
            var created = await _service.GetMdReviewActivitiesAsync(activityId: newId);

            return CreatedAtAction(
                nameof(GetMdReviewActivities),
                new { activityId = newId },
                created
            );
        }

        public sealed class MdReviewLineUpdateRequest
        {
            public string MdDecision { get; set; } = default!;  // "Approved" | "Denied" | "Partial"
            public string Status { get; set; } = default!;      // "Pending" | "InProgress" | "Completed"
            public string? MdNotes { get; set; }
            public int? ReviewedByUserId { get; set; }
            public long? ExpectedVersion { get; set; }          // pass current version from UI if you want OCC
        }

        // PATCH api/AuthActivity/mdreview/{activityId}/lines/{lineId}
        [HttpPatch("mdreview/{activityId:long}/lines/{lineId:long}")]
        public async Task<IActionResult> UpdateMdReviewLine(
            long activityId,
            long lineId,
            [FromBody] MdReviewLineUpdateRequest req)
        {
            if (req is null) return BadRequest("Body is required.");
            if (string.IsNullOrWhiteSpace(req.MdDecision)) return BadRequest("MdDecision is required.");
            if (string.IsNullOrWhiteSpace(req.Status)) return BadRequest("Status is required.");

            var ok = await _service.UpdateMdReviewLineAsync(
                activityId,
                lineId,
                req.MdDecision,
                req.Status,
                req.MdNotes,
                req.ReviewedByUserId,
                req.ExpectedVersion
            );

            // If using optimistic concurrency and version mismatches, repo returns false
            if (!ok) return StatusCode(StatusCodes.Status409Conflict, "Update failed (version mismatch or not found).");

            // Return the refreshed activity with lines so UI can update counters/badges immediately
            var updated = await _service.GetMdReviewActivitiesAsync(activityId: (int)activityId);
            return Ok(updated);
        }
    }
}
