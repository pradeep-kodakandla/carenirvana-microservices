using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using Microsoft.AspNetCore.Mvc;

namespace CareNirvana.Service.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthDetailController : ControllerBase
    {
        private readonly IAuthRepository _authRepo;
        private readonly IAuthNotesRepository _notesRepo;
        private readonly IAuthDocumentsRepository _docsRepo;

        public AuthDetailController(
            IAuthRepository authRepo,
            IAuthNotesRepository notesRepo,
            IAuthDocumentsRepository docsRepo)
        {
            _authRepo = authRepo;
            _notesRepo = notesRepo;
            _docsRepo = docsRepo;
        }

        // ---------------------------
        // Auth Detail APIs
        // ---------------------------

        // GET api/auth/number/{authNumber}
        [HttpGet("number/{authNumber}")]
        public async Task<ActionResult<AuthDetailRow>> GetByNumber(
            string authNumber,
            [FromQuery] bool includeDeleted = false)
        {
            var auth = await _authRepo.GetAuthByNumberAsync(authNumber, includeDeleted);
            if (auth == null) return NotFound();
            return Ok(auth);
        }

        // GET api/auth/{authDetailId}
        [HttpGet("{authDetailId:long}")]
        public async Task<ActionResult<AuthDetailRow>> GetById(
            long authDetailId,
            [FromQuery] bool includeDeleted = false)
        {
            var auth = await _authRepo.GetAuthByIdAsync(authDetailId, includeDeleted);
            if (auth == null) return NotFound();
            return Ok(auth);
        }

        // GET api/auth/member/{memberDetailsId}
        [HttpGet("member/{memberDetailsId:int}")]
        public async Task<ActionResult<List<AuthDetailRow>>> GetByMember(
            int memberDetailsId,
            [FromQuery] bool includeDeleted = false)
        {
            var list = await _authRepo.GetAuthsByMemberAsync(memberDetailsId, includeDeleted);
            return Ok(list);
        }

        // POST api/auth?userId=123
        [HttpPost]
        public async Task<ActionResult<long>> Create(
            [FromBody] CreateAuthRequest req,
            [FromQuery] int userId)
        {
            var id = await _authRepo.CreateAuthAsync(req, userId);
            return CreatedAtAction(nameof(GetById), new { authDetailId = id }, id);
        }

        // PUT api/auth/{authDetailId}?userId=123
        [HttpPut("{authDetailId:long}")]
        public async Task<IActionResult> Update(
            long authDetailId,
            [FromBody] UpdateAuthRequest req,
            [FromQuery] int userId)
        {
            await _authRepo.UpdateAuthAsync(authDetailId, req, userId);
            return NoContent();
        }

        // DELETE api/auth/{authDetailId}?userId=123
        [HttpDelete("{authDetailId:long}")]
        public async Task<IActionResult> SoftDelete(
            long authDetailId,
            [FromQuery] int userId)
        {
            await _authRepo.SoftDeleteAuthAsync(authDetailId, userId);
            return NoContent();
        }

        // ---------------------------
        // Notes APIs
        // ---------------------------

        // GET api/auth/{authDetailId}/notes
        [HttpGet("{authDetailId:long}/notes")]
        public async Task<ActionResult<IReadOnlyList<AuthNoteDto>>> GetNotes(
            long authDetailId,
            CancellationToken ct)
        {
            var notes = await _notesRepo.GetNotesAsync(authDetailId, ct);
            return Ok(notes);
        }

        // POST api/auth/{authDetailId}/notes?userId=123
        [HttpPost("{authDetailId:long}/notes")]
        public async Task<ActionResult<Guid>> CreateNote(
            long authDetailId,
            [FromBody] CreateAuthNoteRequest req,
            [FromQuery] int userId,
            CancellationToken ct)
        {
            var noteId = await _notesRepo.InsertNoteAsync(authDetailId, req, userId, ct);
            return Ok(noteId);
        }

        // PUT api/auth/{authDetailId}/notes/{noteId}?userId=123
        [HttpPut("{authDetailId:long}/notes/{noteId:guid}")]
        public async Task<IActionResult> UpdateNote(
            long authDetailId,
            Guid noteId,
            [FromBody] UpdateAuthNoteRequest req,
            [FromQuery] int userId,
            CancellationToken ct)
        {
            var ok = await _notesRepo.UpdateNoteAsync(authDetailId, noteId, req, userId, ct);
            return ok ? NoContent() : NotFound();
        }

        // DELETE api/auth/{authDetailId}/notes/{noteId}?userId=123
        [HttpDelete("{authDetailId:long}/notes/{noteId:guid}")]
        public async Task<IActionResult> DeleteNote(
            long authDetailId,
            Guid noteId,
            [FromQuery] int userId,
            CancellationToken ct)
        {
            var ok = await _notesRepo.SoftDeleteNoteAsync(authDetailId, noteId, userId, ct);
            return ok ? NoContent() : NotFound();
        }

        // ---------------------------
        // Documents APIs
        // ---------------------------

        // GET api/auth/{authDetailId}/documents
        [HttpGet("{authDetailId:long}/documents")]
        public async Task<ActionResult<IReadOnlyList<AuthDocumentDto>>> GetDocuments(
            long authDetailId,
            CancellationToken ct)
        {
            var docs = await _docsRepo.GetDocumentsAsync(authDetailId, ct);
            return Ok(docs);
        }

        // POST api/auth/{authDetailId}/documents?userId=123
        [HttpPost("{authDetailId:long}/documents")]
        public async Task<ActionResult<Guid>> CreateDocument(
            long authDetailId,
            [FromBody] CreateAuthDocumentRequest req,
            [FromQuery] int userId,
            CancellationToken ct)
        {
            var documentId = await _docsRepo.InsertDocumentAsync(authDetailId, req, userId, ct);
            return Ok(documentId);
        }

        // PUT api/auth/{authDetailId}/documents/{documentId}?userId=123
        [HttpPut("{authDetailId:long}/documents/{documentId:guid}")]
        public async Task<IActionResult> UpdateDocument(
            long authDetailId,
            Guid documentId,
            [FromBody] UpdateAuthDocumentRequest req,
            [FromQuery] int userId,
            CancellationToken ct)
        {
            var ok = await _docsRepo.UpdateDocumentAsync(authDetailId, documentId, req, userId, ct);
            return ok ? NoContent() : NotFound();
        }

        // DELETE api/auth/{authDetailId}/documents/{documentId}?userId=123
        [HttpDelete("{authDetailId:long}/documents/{documentId:guid}")]
        public async Task<IActionResult> DeleteDocument(
            long authDetailId,
            Guid documentId,
            [FromQuery] int userId,
            CancellationToken ct)
        {
            var ok = await _docsRepo.SoftDeleteDocumentAsync(authDetailId, documentId, userId, ct);
            return ok ? NoContent() : NotFound();
        }

        [HttpGet("template/{authTemplateId}/decision")]
        public async Task<ActionResult<TemplateSectionsResponse?>> GetDecisionTemplate(
            int authTemplateId,
            CancellationToken ct)
        {
            var template = await _authRepo.GetDecisionTemplateAsync(authTemplateId, ct);
            if (template == null) return NotFound();
            return Ok(template);
        }

        [HttpGet("template/{authTemplateId}/notes")]
        public async Task<ActionResult<TemplateSectionResponse?>> GetAuthNotesTemplate(
            int authTemplateId,
            CancellationToken ct)
        {
            var template = await _notesRepo.GetAuthNotesTemplateAsync(authTemplateId, ct);
            if (template == null) return NotFound();
            return Ok(template);
        }
        [HttpGet("template/{authTemplateId}/documents")]
        public async Task<ActionResult<TemplateSectionResponse?>> GetAuthDocumentsTemplate(
            int authTemplateId,
            CancellationToken ct)
        {
            var template = await _docsRepo.GetAuthDocumentsTemplateAsync(authTemplateId, ct);
            if (template == null) return NotFound();
            return Ok(template);
        }
    }
}
