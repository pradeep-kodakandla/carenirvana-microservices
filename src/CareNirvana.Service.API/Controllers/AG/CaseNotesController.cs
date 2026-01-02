using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using Microsoft.AspNetCore.Mvc;

namespace CareNirvana.Service.API.Controllers.AG
{
    [Route("api/[controller]")]
    [ApiController]
    public class CaseNotesController : ControllerBase
    {
        private readonly ICaseNotesRepository _notesRepo;

        public CaseNotesController(ICaseNotesRepository notesRepo)
        {
            _notesRepo = notesRepo;
        }

        [HttpGet("case-templates/{caseTemplateId:int}/sections/case-notes")]
        public async Task<ActionResult<CaseNotesTemplateResponse>> GetCaseNotesTemplate(
        int caseTemplateId,
        CancellationToken ct)
        {
            var result = await _notesRepo.GetCaseNotesTemplateAsync(caseTemplateId, ct);
            if (result is null) return NotFound();
            return Ok(result);
        }

        // GET /api/cases/{caseHeaderId}/levels/{levelId}/notes
        [HttpGet("cases/{caseHeaderId:int}/levels/{levelId:int}/notes")]
        public async Task<ActionResult<CaseNotesResponse>> GetNotes(
            int caseHeaderId,
            int levelId,
            CancellationToken ct)
        {
            var notes = await _notesRepo.GetNotesAsync(caseHeaderId, levelId, ct);

            return Ok(new CaseNotesResponse
            {
                CaseHeaderId = caseHeaderId,
                LevelId = levelId,
                Notes = new System.Collections.Generic.List<CaseNoteDto>(notes)
            });
        }

        // POST /api/cases/{caseHeaderId}/levels/{levelId}/notes
        [HttpPost("cases/{caseHeaderId:int}/levels/{levelId:int}/notes")]
        public async Task<ActionResult<object>> InsertNote(
            int caseHeaderId,
            int levelId,
            [FromBody] CreateCaseNoteRequest req,
            CancellationToken ct)
        {
            if (req is null) return BadRequest("Request body is required.");
            if (string.IsNullOrWhiteSpace(req.NoteText)) return BadRequest("NoteText is required.");

            var userId = GetUserId(); // <-- replace with your auth/session logic

            var noteId = await _notesRepo.InsertNoteAsync(caseHeaderId, levelId, req, userId, ct);

            // 201 Created with location to the note
            return CreatedAtAction(
                nameof(GetNoteById),
                new { caseHeaderId, levelId, noteId },
                new { noteId }
            );
        }

        // GET /api/cases/{caseHeaderId}/levels/{levelId}/notes/{noteId}
        // (Optional helper endpoint used by CreatedAtAction)
        [HttpGet("cases/{caseHeaderId:int}/levels/{levelId:int}/notes/{noteId:guid}")]
        public async Task<ActionResult<CaseNoteDto>> GetNoteById(
            int caseHeaderId,
            int levelId,
            Guid noteId,
            CancellationToken ct)
        {
            var notes = await _notesRepo.GetNotesAsync(caseHeaderId, levelId, ct);
            var note = System.Linq.Enumerable.FirstOrDefault(notes, n => n.NoteId == noteId);
            if (note is null) return NotFound();
            return Ok(note);
        }

        // PUT /api/cases/{caseHeaderId}/levels/{levelId}/notes/{noteId}
        [HttpPut("cases/{caseHeaderId:int}/levels/{levelId:int}/notes/{noteId:guid}")]
        public async Task<IActionResult> UpdateNote(
            int caseHeaderId,
            int levelId,
            Guid noteId,
            [FromBody] UpdateCaseNoteRequest req,
            CancellationToken ct)
        {
            if (req is null) return BadRequest("Request body is required.");

            var userId = GetUserId();

            var updated = await _notesRepo.UpdateNoteAsync(caseHeaderId, levelId, noteId, req, userId, ct);
            if (!updated) return NotFound(); // if you keep "rows > 0" semantics, you may prefer Ok()

            return NoContent();
        }

        // DELETE /api/cases/{caseHeaderId}/levels/{levelId}/notes/{noteId}
        [HttpDelete("cases/{caseHeaderId:int}/levels/{levelId:int}/notes/{noteId:guid}")]
        public async Task<IActionResult> DeleteNote(
            int caseHeaderId,
            int levelId,
            Guid noteId,
            CancellationToken ct)
        {
            var userId = GetUserId();

            var deleted = await _notesRepo.SoftDeleteNoteAsync(caseHeaderId, levelId, noteId, userId, ct);
            if (!deleted) return NotFound();

            return NoContent();
        }

        // Replace this with your real user resolution (JWT claims, session, etc.)
        private int GetUserId()
        {
            // Example: int.Parse(User.FindFirst("userId")!.Value);
            // For now:
            return int.TryParse(HttpContext?.Request?.Headers["x-userid"], out var id) ? id : 0;
        }
    }
}
