using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using CareNirvana.Service.Infrastructure.Repository;
using Microsoft.AspNetCore.Mvc;

namespace CareNirvana.Service.API.Controllers.Admin
{
    [Route("api/[controller]")]
    [ApiController]
    public class CfgvalidationController : ControllerBase
    {
        private readonly ICfgvalidationRepository _repo;

        public CfgvalidationController(ICfgvalidationRepository repo)
        {
            _repo = repo;
        }

        // GET /api/Cfgvalidation/modules/{moduleId}/validations
        [HttpGet("modules/{moduleId:int}/validations")]
        public async Task<ActionResult<IEnumerable<Cfgvalidation>>> GetAllByModule(
            int moduleId,
            CancellationToken ct) // keeping CT like your pattern (not used by repo)
        {
            var result = await _repo.GetAllAsync(moduleId);
            return Ok(result);
        }

        // GET /api/Cfgvalidation/validations/{validationId}
        [HttpGet("validations/{validationId:int}")]
        public async Task<ActionResult<Cfgvalidation>> GetById(
            int validationId,
            CancellationToken ct)
        {
            var result = await _repo.GetByIdAsync(validationId);
            if (result is null) return NotFound();
            return Ok(result);
        }

        // POST /api/Cfgvalidation/validations
        [HttpPost("validations")]
        public async Task<ActionResult<object>> Insert(
            [FromBody] Cfgvalidation req,
            CancellationToken ct)
        {
            if (req is null) return BadRequest("Request body is required.");
            if (req.moduleId <= 0) return BadRequest("moduleId is required.");
            if (string.IsNullOrWhiteSpace(req.validationJson)) return BadRequest("validationJson is required.");

            var userId = GetUserId();
            req.createdBy = userId;
            req.activeFlag = true;

            var created = await _repo.InsertAsync(req);

            return CreatedAtAction(
                nameof(GetById),
                new { validationId = created.validationId },
                new { validationId = created.validationId }
            );
        }

        // PUT /api/Cfgvalidation/validations/{validationId}
        [HttpPut("validations/{validationId:int}")]
        public async Task<IActionResult> Update(
            int validationId,
            [FromBody] Cfgvalidation req,
            CancellationToken ct)
        {
            if (req is null) return BadRequest("Request body is required.");

            var existing = await _repo.GetByIdAsync(validationId);
            if (existing is null) return NotFound();

            var userId = GetUserId();

            // ensure id is from route
            req.validationId = validationId;
            req.updatedBy = userId;

            // Preserve created fields if caller didn’t send them
            req.createdBy ??= existing.createdBy;
            req.createdOn ??= existing.createdOn;

            await _repo.UpdateAsync(req);
            return NoContent();
        }

        // DELETE /api/Cfgvalidation/validations/{validationId} (soft delete)
        [HttpDelete("validations/{validationId:int}")]
        public async Task<IActionResult> Delete(
            int validationId,
            CancellationToken ct)
        {
            var userId = GetUserId();

            var deleted = await _repo.DeleteAsync(validationId, userId);
            if (!deleted) return NotFound();

            return NoContent();
        }

        [HttpGet("modules/{moduleId:int}/primary-template")]
        public async Task<IActionResult> GetPrimaryTemplate(int moduleId)
        {
            var json = await _repo.GetPrimaryTemplateJsonAsync(moduleId);
            if (string.IsNullOrWhiteSpace(json)) return NotFound();

            // Return raw JSON (Angular gets it as object)
            return Content(json, "application/json");
        }

        private int GetUserId()
        {
            return int.TryParse(HttpContext?.Request?.Headers["x-userid"], out var id) ? id : 0;
        }
    }
}
