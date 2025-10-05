using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using Microsoft.AspNetCore.Mvc;

namespace CareNirvana.Service.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MemberProgramController : ControllerBase
    {
        private readonly IMemberProgramRepository _repository;

        public MemberProgramController(IMemberProgramRepository repository)
        {
            _repository = repository;
        }

        // ✅ Create
        [HttpPost("create")]
        public async Task<IActionResult> CreateAsync([FromBody] MemberProgram request)
        {
            if (request == null)
                return BadRequest("Invalid payload.");

            request.CreatedOn = DateTime.UtcNow;
            var id = await _repository.InsertMemberProgramAsync(request);
            return Ok(new { memberProgramId = id });
        }

        // ✅ Update
        [HttpPut("update")]
        public async Task<IActionResult> UpdateAsync([FromBody] MemberProgram request)
        {
            if (request.MemberProgramId <= 0)
                return BadRequest("Invalid MemberProgramId.");

            request.UpdatedOn = DateTime.UtcNow;
            var rows = await _repository.UpdateMemberProgramAsync(request);
            return Ok(new { updatedCount = rows });
        }

        // ✅ Soft Delete
        [HttpDelete("delete/{id:int}")]
        public async Task<IActionResult> DeleteAsync(int id, [FromQuery] int deletedBy)
        {
            if (id <= 0)
                return BadRequest("Invalid id.");

            var rows = await _repository.SoftDeleteMemberProgramAsync(id, deletedBy);
            return Ok(new { deletedCount = rows });
        }

        // ✅ Get by ID
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetByIdAsync(int id)
        {
            var data = await _repository.GetMemberProgramByIdAsync(id);
            if (data == null)
                return NotFound();

            return Ok(data);
        }

        // ✅ Get list for Member (paged)
        [HttpGet("list/{memberDetailsId:int}")]
        public async Task<IActionResult> GetListAsync(
            int memberDetailsId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25,
            [FromQuery] bool includeDeleted = false)
        {
            var (items, total) = await _repository.GetMemberProgramsForMemberAsync(memberDetailsId, page, pageSize, includeDeleted);
            return Ok(new { items, total });
        }

        // ✅ Get active list for Member (non-paged)
        [HttpGet("active/{memberDetailsId:int}")]
        public async Task<IActionResult> GetActiveAsync(int memberDetailsId)
        {
            var list = await _repository.GetActiveMemberProgramsForMemberAsync(memberDetailsId);
            return Ok(list);
        }
    }
}
