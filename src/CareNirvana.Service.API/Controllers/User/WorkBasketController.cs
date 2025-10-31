using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
public class WorkBasketController : ControllerBase
{
    private readonly IWorkBasket _repo;

    public WorkBasketController(IWorkBasket repo) => _repo = repo;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
       => Ok(await _repo.GetAllAsync(includeInactive));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var wb = await _repo.GetByIdAsync(id);
        if (wb is null) return NotFound();

        var groupIds = await _repo.GetLinkedWorkGroupIdsAsync(id);
        return Ok(new WorkBasketView
        {
            WorkBasketId = wb.WorkBasketId,
            WorkBasketCode = wb.WorkBasketCode,
            WorkBasketName = wb.WorkBasketName,
            Description = wb.Description,
            ActiveFlag = wb.ActiveFlag,
            WorkGroupIds = groupIds
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] WorkBasketCreateDto dto)
    {
        // Optionally enforce uniqueness right here using repo helpers
        if (await _repo.ExistsByNameAsync(dto.WorkBasketName))
            return Conflict($"WorkBasketName '{dto.WorkBasketName}' already exists.");
        if (await _repo.ExistsByCodeAsync(dto.WorkBasketCode))
            return Conflict($"WorkBasketCode '{dto.WorkBasketCode}' already exists.");

        var id = await _repo.CreateWithGroupsAsync(new WorkBasket
        {
            WorkBasketCode = dto.WorkBasketCode,
            WorkBasketName = dto.WorkBasketName,
            Description = dto.Description,
            CreatedBy = dto.CreatedBy,
            ActiveFlag = true
        }, dto.WorkGroupIds ?? Enumerable.Empty<int>());

        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] WorkBasketUpdateDto dto)
    {
        if (id != dto.WorkBasketId) return BadRequest("Mismatched id.");

        if (await _repo.ExistsByNameAsync(dto.WorkBasketName, dto.WorkBasketId))
            return Conflict($"WorkBasketName '{dto.WorkBasketName}' already exists.");
        if (await _repo.ExistsByCodeAsync(dto.WorkBasketCode, dto.WorkBasketId))
            return Conflict($"WorkBasketCode '{dto.WorkBasketCode}' already exists.");

        var rows = await _repo.UpdateWithGroupsAsync(new WorkBasket
        {
            WorkBasketId = dto.WorkBasketId,
            WorkBasketCode = dto.WorkBasketCode,
            WorkBasketName = dto.WorkBasketName,
            Description = dto.Description,
            ActiveFlag = dto.ActiveFlag,
            UpdatedBy = dto.UpdatedBy
        }, dto.WorkGroupIds ?? Enumerable.Empty<int>());

        if (rows == 0) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> SoftDelete(int id, [FromQuery] string deletedBy)
    {
        var rows = await _repo.SoftDeleteAsync(id, deletedBy);
        if (rows == 0) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id:int}/hard")]
    public async Task<IActionResult> HardDelete(int id)
    {
        var rows = await _repo.HardDeleteAsync(id);
        if (rows == 0) return NotFound();
        return NoContent();
    }

    [HttpGet("user/{userId:int}")]
    public async Task<IActionResult> GetUserWorkGroups(int userId)
        => Ok(await _repo.GetUserWorkGroupsAsync(userId));

}
