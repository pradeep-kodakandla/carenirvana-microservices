using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
public class WorkGroupController : ControllerBase
{
    private readonly IWorkGroup _service;

    public WorkGroupController(IWorkGroup service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
        => Ok(await _service.GetAllAsync(includeInactive));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _service.GetByIdAsync(id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] WorkGroupCreateDto dto)
    {
        var id = await _service.CreateAsync(new WorkGroup
        {
            WorkGroupCode = dto.WorkGroupCode,
            WorkGroupName = dto.WorkGroupName,
            Description = dto.Description,
            IsFax = dto.IsFax,
            IsProviderPortal = dto.IsProviderPortal,
            CreatedBy = dto.CreatedBy
        });
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] WorkGroupUpdateDto dto)
    {
        if (id != dto.WorkGroupId) return BadRequest("Mismatched id.");
        await _service.UpdateAsync(new WorkGroup
        {
            WorkGroupId = dto.WorkGroupId,
            WorkGroupCode = dto.WorkGroupCode,
            WorkGroupName = dto.WorkGroupName,
            Description = dto.Description,
            IsFax = dto.IsFax,
            IsProviderPortal = dto.IsProviderPortal,
            ActiveFlag = dto.ActiveFlag,
            UpdatedBy = dto.UpdatedBy
        });
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> SoftDelete(int id, [FromQuery] string deletedBy)
    {
        await _service.SoftDeleteAsync(id, deletedBy);
        return NoContent();
    }

    [HttpPost("{id:int}/restore")]
    public async Task<IActionResult> Restore(int id, [FromQuery] string updatedBy)
    {
        await _service.RestoreAsync(id, updatedBy);
        return NoContent();
    }

    [HttpDelete("{id:int}/hard")]
    public async Task<IActionResult> HardDelete(int id)
    {
        await _service.HardDeleteAsync(id);
        return NoContent();
    }
}
