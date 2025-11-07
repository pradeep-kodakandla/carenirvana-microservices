using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[Route("api/[controller]")]
[ApiController]
public class MessageController : ControllerBase
{
    private readonly IMessageRepository _repo;

    public MessageController(IMessageRepository repo) => _repo = repo;

    // GET /api/messages?userId=10&page=1&pageSize=50   OR
    // GET /api/messages?memberDetailsId=123&page=1&pageSize=50
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int? userId, [FromQuery] int? memberDetailsId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        if (userId.HasValue)
        {
            var byUser = await _repo.GetByUserAsync(userId.Value, page <= 0 ? 1 : page, pageSize <= 0 ? 50 : pageSize);
            return Ok(byUser);
        }

        if (memberDetailsId.HasValue)
        {
            var byMember = await _repo.GetByMemberAsync(memberDetailsId.Value, page <= 0 ? 1 : page, pageSize <= 0 ? 50 : pageSize);
            return Ok(byMember);
        }

        return BadRequest("Provide either userId or memberDetailsId.");
    }

    // Convenience: GET /api/messages/user/10?page=1&pageSize=50
    [HttpGet("user/{userId:int}")]
    public async Task<IActionResult> GetByUser(int userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var threads = await _repo.GetByUserAsync(userId, page <= 0 ? 1 : page, pageSize <= 0 ? 50 : pageSize);
        return Ok(threads);
    }

    // Convenience: GET /api/messages/member/123?page=1&pageSize=50
    [HttpGet("member/{memberDetailsId:int}")]
    public async Task<IActionResult> GetByMember(int memberDetailsId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var threads = await _repo.GetByMemberAsync(memberDetailsId, page <= 0 ? 1 : page, pageSize <= 0 ? 50 : pageSize);
        return Ok(threads);
    }

    // GET /api/messages/thread/1001
    [HttpGet("thread/{threadId:long}")]
    public async Task<IActionResult> GetThread(long threadId)
    {
        var thread = await _repo.GetThreadAsync(threadId);
        if (thread is null) return NotFound();
        return Ok(thread);
    }

    // POST /api/messages
    // Body: { "otherUserId": 42, "memberDetailsId": 123, "parentMessageId": null, "body": "hi" }
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMessageRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Body))
            return BadRequest("Body is required.");

        //var currentUserIdClaim = User.FindFirst("userid")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //if (!int.TryParse(currentUserIdClaim, out var currentUserId))
        //    return Forbid();

        var threadId = await _repo.EnsureThreadAsync(request.CreatedUserId, request.OtherUserId, request.MemberDetailsId);
        var messageId = await _repo.CreateMessageAsync(request.CreatedUserId, threadId, request.Body, request.ParentMessageId, request.Subject);
        var thread = await _repo.GetThreadAsync(threadId);

        // Created with location of the thread
        return Created($"/api/messages/thread/{threadId}", new { messageId, thread });
    }

    // PUT /api/messages/5001   Body: { "body": "updated text" }
    [HttpPut("{messageId:long}")]
    public async Task<IActionResult> Update(long messageId, [FromBody] UpdateMessageRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Body))
            return BadRequest("Body is required.");

        //var editorUserIdClaim = User.FindFirst("userid")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //if (!int.TryParse(editorUserIdClaim, out var editorUserId))
        //    return Forbid();

        var changed = await _repo.UpdateMessageAsync(messageId, request.CreatedUserId, request.Body);
        return changed == 0 ? NotFound() : NoContent();
    }

    // DELETE /api/messages/5001  (soft delete)
    [HttpDelete("{messageId:long}")]
    public async Task<IActionResult> Delete(long messageId)
    {
        var changed = await _repo.DeleteMessageAsync(messageId);
        return changed == 0 ? NotFound() : NoContent();
    }

}

