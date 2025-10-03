
using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
public class MemberHealthNotesController : ControllerBase
{
    private readonly IMemberNotes _MemberhealthnotesService;

    public MemberHealthNotesController(IMemberNotes memberNotesService)
    {
        _MemberhealthnotesService = memberNotesService;
    }
    [HttpPost]
    public async Task<IActionResult> InsertMemberHealthNote([FromBody] MemberHealthNote note)
    {
        if (note == null)
            return BadRequest(new { message = "Invalid note data." });
        var newId = await _MemberhealthnotesService.InsertMemberHealthNoteAsync(note);
        return CreatedAtAction(nameof(GetMemberHealthNoteById), new { id = newId }, new { id = newId });
    }
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateMemberHealthNote(long id, [FromBody] MemberHealthNote note)
    {
        if (note == null || id != note.MemberHealthNotesId)
            return BadRequest(new { message = "Invalid note data." });
        var existingNote = await _MemberhealthnotesService.GetMemberHealthNoteByIdAsync(id);
        if (existingNote == null)
            return NotFound(new { message = "Note not found." });
        var rowsAffected = await _MemberhealthnotesService.UpdateMemberHealthNoteAsync(note);
        if (rowsAffected == 0)
            return StatusCode(500, new { message = "Failed to update the note." });
        return NoContent();
    }
    [HttpDelete("{id}")]    
    public async Task<IActionResult> SoftDeleteMemberHealthNote(long id, [FromQuery] int deletedBy)
    {
        var existingNote = await _MemberhealthnotesService.GetMemberHealthNoteByIdAsync(id);
        if (existingNote == null)
            return NotFound(new { message = "Note not found." });
        var rowsAffected = await _MemberhealthnotesService.SoftDeleteMemberHealthNoteAsync(id, deletedBy);
        if (rowsAffected == 0)
            return StatusCode(500, new { message = "Failed to delete the note." });
        return NoContent();
    }
    [HttpGet("{id}")]
    public async Task<IActionResult> GetMemberHealthNoteById(long id)
    {
        var note = await _MemberhealthnotesService.GetMemberHealthNoteByIdAsync(id);
        if (note == null)
            return NotFound(new { message = "Note not found." });
        return Ok(note);
    }
    [HttpGet("member/{memberId}")]
    public async Task<IActionResult> GetMemberHealthNotesForMember(long memberId, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, [FromQuery] bool includeDeleted = false)
    {
        if (page <= 0 || pageSize <= 0)
            return BadRequest(new { message = "Page and pageSize must be greater than zero." });
        var (items, total) = await _MemberhealthnotesService.GetMemberHealthNotesForMemberAsync(memberId, page, pageSize, includeDeleted);
        var result = new
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        };
        return Ok(result);
    }
    [HttpGet("member/{memberId}/alerts")]
    public async Task<IActionResult> GetActiveAlertsForMember(long memberId)
    {
        var alerts = await _MemberhealthnotesService.GetActiveAlertsForMemberAsync(memberId);
        return Ok(alerts);
    }

}