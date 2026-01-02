using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using Microsoft.AspNetCore.Mvc;

namespace CareNirvana.Service.API.Controllers.AG
{
    [Route("api/[controller]")]
    [ApiController]

    public class CaseDocumentsController : ControllerBase
    {
        private readonly ICaseDocumentsRepository _repo;

        public CaseDocumentsController(ICaseDocumentsRepository repo) => _repo = repo;

        [HttpGet("case-templates/{caseTemplateId:int}/sections/case-documents")]
        public async Task<ActionResult<CaseDocumentsTemplateResponse>> GetTemplate(int caseTemplateId, CancellationToken ct)
        {
            var res = await _repo.GetCaseDocumentsTemplateAsync(caseTemplateId, ct);
            return res is null ? NotFound() : Ok(res);
        }

        [HttpGet("cases/{caseHeaderId:int}/levels/{levelId:int}/documents")]
        public async Task<ActionResult<CaseDocumentsResponse>> GetDocuments(int caseHeaderId, int levelId, CancellationToken ct)
        {
            var docs = await _repo.GetDocumentsAsync(caseHeaderId, levelId, ct);
            return Ok(new CaseDocumentsResponse { CaseHeaderId = caseHeaderId, LevelId = levelId, Documents = docs.ToList() });
        }

        [HttpPost("cases/{caseHeaderId:int}/levels/{levelId:int}/documents")]
        public async Task<ActionResult<object>> Create(int caseHeaderId, int levelId, [FromBody] CreateCaseDocumentRequest req, CancellationToken ct)
        {
            var userId = GetUserId();
            var documentId = await _repo.InsertDocumentAsync(caseHeaderId, levelId, req, userId, ct);
            return Ok(new { documentId });
        }

        [HttpPut("cases/{caseHeaderId:int}/levels/{levelId:int}/documents/{documentId:guid}")]
        public async Task<IActionResult> Update(int caseHeaderId, int levelId, Guid documentId, [FromBody] UpdateCaseDocumentRequest req, CancellationToken ct)
        {
            var userId = GetUserId();
            var ok = await _repo.UpdateDocumentAsync(caseHeaderId, levelId, documentId, req, userId, ct);
            return ok ? NoContent() : NotFound();
        }

        [HttpDelete("cases/{caseHeaderId:int}/levels/{levelId:int}/documents/{documentId:guid}")]
        public async Task<IActionResult> Delete(int caseHeaderId, int levelId, Guid documentId, CancellationToken ct)
        {
            var userId = GetUserId();
            var ok = await _repo.SoftDeleteDocumentAsync(caseHeaderId, levelId, documentId, userId, ct);
            return ok ? NoContent() : NotFound();
        }

        private int GetUserId()
            => int.TryParse(HttpContext?.Request?.Headers["x-userid"], out var id) ? id : 0;
    }
}