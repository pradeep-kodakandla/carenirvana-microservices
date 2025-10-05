using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using Microsoft.AspNetCore.Mvc;

namespace CareNirvana.Service.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MemberCareGiverController : ControllerBase
    {
        private readonly IMemberCareGiverRepository _repo;

        public MemberCareGiverController(IMemberCareGiverRepository repository)
        {
            _repo = repository;
        }

        /// <summary>
        /// Get ALL caregivers (and their addresses/phones/languages/portal) for a member.
        /// </summary>
        [HttpGet("by-member/{memberDetailsId:int}")]
        public async Task<ActionResult<IReadOnlyList<MemberCaregiverDto>>> GetByMemberDetailsId([FromRoute] int memberDetailsId)
        {
            var data = await _repo.GetBundleByMemberDetailsIdAsync(memberDetailsId);
            return Ok(data);
        }

        /// <summary>
        /// Get single caregiver (just the caregiver row).
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<MemberCaregiver>> GetById([FromRoute] int id)
        {
            var c = await _repo.GetByIdAsync(id);
            if (c is null) return NotFound();
            return Ok(c);
        }

        /// <summary>
        /// Create caregiver.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<int>> Create([FromBody] MemberCaregiver model)
        {
            if (model is null) return BadRequest("Body required.");
            // minimal validation (extend as you need)
            if (model.MemberDetailsId is null) return BadRequest("memberdetailsid is required.");

            model.CreatedOn ??= DateTime.UtcNow;
            var id = await _repo.CreateAsync(model);
            return CreatedAtAction(nameof(GetById), new { id }, id);
        }

        /// <summary>
        /// Update caregiver.
        /// </summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] MemberCaregiver model)
        {
            if (model is null) return BadRequest("Body required.");
            model.MemberCaregiverId = id;
            model.UpdatedOn ??= DateTime.UtcNow;

            var ok = await _repo.UpdateAsync(model);
            if (!ok) return NotFound();
            return NoContent();
        }

        /// <summary>
        /// Soft delete caregiver.
        /// </summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete([FromRoute] int id, [FromQuery] int deletedBy)
        {
            var ok = await _repo.SoftDeleteAsync(id, deletedBy);
            if (!ok) return NotFound();
            return NoContent();
        }
    }
}
