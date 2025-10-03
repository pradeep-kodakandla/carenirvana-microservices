
using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]

public class MemberDocumentController : ControllerBase
{
    private readonly IMemberDocument _MemberDocumentService;

    public MemberDocumentController(IMemberDocument memberDocumentService)
    {
        _MemberDocumentService = memberDocumentService;
    }
    [HttpPost]
    public async Task<IActionResult> InsertMemberDocument([FromBody] MemberDocument doc)
    {
        if (doc == null)
            return BadRequest(new { message = "Invalid document data." });
        var newId = await _MemberDocumentService.InsertMemberDocumentAsync(doc);
        return CreatedAtAction(nameof(GetMemberDocumentById), new { id = newId }, new { id = newId });
    }
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateMemberDocument(long id, [FromBody] MemberDocument doc)
    {
        if (doc == null || id != doc.MemberDocumentId)
            return BadRequest(new { message = "Invalid document data." });
        var existingDoc = await _MemberDocumentService.GetMemberDocumentByIdAsync(id);
        if (existingDoc == null)
            return NotFound(new { message = "Document not found." });
        var rowsAffected = await _MemberDocumentService.UpdateMemberDocumentAsync(doc);
        if (rowsAffected == 0)
            return StatusCode(500, new { message = "Failed to update the document." });
        return NoContent();
    }
    [HttpDelete("{id}")]
    public async Task<IActionResult> SoftDeleteMemberDocument(long id, [FromQuery] int deletedBy)
    {
        var existingDoc = await _MemberDocumentService.GetMemberDocumentByIdAsync(id);
        if (existingDoc == null)
            return NotFound(new { message = "Document not found." });
        var rowsAffected = await _MemberDocumentService.SoftDeleteMemberDocumentAsync(id, deletedBy);
        if (rowsAffected == 0)
            return StatusCode(500, new { message = "Failed to delete the document." });
        return NoContent();
    }
    [HttpGet("{id}")]
    public async Task<IActionResult> GetMemberDocumentById(long id)
    {
        var doc = await _MemberDocumentService.GetMemberDocumentByIdAsync(id);
        if (doc == null)
            return NotFound(new { message = "Document not found." });
        return Ok(doc);
    }
    [HttpGet("member/{memberId}")]
    public async Task<IActionResult> GetMemberDocumentsForMember(long memberId, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, [FromQuery] bool includeDeleted = false)
    {
        if (page <= 0 || pageSize <= 0)
            return BadRequest(new { message = "Page and pageSize must be greater than zero." });
        var (items, total) = await _MemberDocumentService.GetMemberDocumentsForMemberAsync(memberId, page, pageSize, includeDeleted);
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

}

