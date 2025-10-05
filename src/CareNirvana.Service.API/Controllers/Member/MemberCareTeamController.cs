using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using Microsoft.AspNetCore.Mvc;

namespace CareNirvana.Service.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MemberCareTeamController : ControllerBase
    {
        private readonly IMemberCareTeamRepository _repo;

        public MemberCareTeamController(IMemberCareTeamRepository repository)
        {
            _repo = repository;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<MemberCareStaffView>>> List(
            [FromQuery] int? userId,
            [FromQuery] int? memberDetailsId,
            [FromQuery] bool includeInactive = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25,
            [FromQuery] string? search = null)
        {
            var res = await _repo.ListAsync(userId, memberDetailsId, includeInactive, page, pageSize, search);
            return Ok(res);
        }

        /// <summary>
        /// Get one by id with joined names.
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<MemberCareStaffView>> Get(int id)
        {
            var item = await _repo.GetAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        /// <summary>
        /// Create a member-care-staff assignment.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> Create([FromBody] MemberCareStaffCreateRequest req)
        {
            if (req == null) return BadRequest();
            var id = await _repo.CreateAsync(req);
            var created = await _repo.GetAsync(id);
            return CreatedAtAction(nameof(Get), new { id }, created);
        }

        /// <summary>
        /// Update an assignment (partial allowed).
        /// </summary>
        [HttpPut("{id:int}")]
        public async Task<ActionResult> Update(int id, [FromBody] MemberCareStaffUpdateRequest req)
        {
            if (req == null) return BadRequest();
            var ok = await _repo.UpdateAsync(id, req);
            if (!ok) return NotFound();
            var updated = await _repo.GetAsync(id);
            return Ok(updated);
        }

        /// <summary>
        /// Soft delete: sets ActiveFlag=false and stamps DeletedOn/By.
        /// </summary>
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id, [FromQuery] int? deletedBy = null)
        {
            var ok = await _repo.SoftDeleteAsync(id, deletedBy);
            if (!ok) return NotFound();
            return NoContent();
        }
    }
}
