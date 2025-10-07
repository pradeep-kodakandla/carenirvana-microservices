using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using Microsoft.AspNetCore.Mvc;

namespace CareNirvana.Service.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MemberAlertController : ControllerBase
    {
        private readonly IMemberAlertRepository _repository;

        public MemberAlertController(IMemberAlertRepository repository)
        {
            _repository = repository;
        }
        [HttpGet]
        public async Task<ActionResult<MemberAlertPagedResult>> Get(
        [FromQuery] int[]? memberDetailsId,
        [FromQuery] int? alertId,
        [FromQuery] bool activeOnly = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
        {
            var result = await _repository.GetAlertsAsync(
                memberDetailsIds: memberDetailsId,
                alertId: alertId,
                activeOnly: activeOnly,
                page: page,
                pageSize: pageSize);

            return Ok(result);
        }

        [HttpPut("{id:int}/status")]
        public async Task<IActionResult> UpdateStatus( int id,  [FromBody] UpdateAlertStatusDto dto)
        {
            var result = await _repository.UpdateAlertStatusAsync(
                memberAlertId: id,
                alertStatusId: dto.AlertStatusId,
                dismissedDate: dto.DismissedDate,
                acknowledgedDate: dto.AcknowledgedDate,
                updatedBy: dto.UpdatedBy ?? 0);

            if (result == null)
                return NotFound(new { message = "Alert not found or inactive." });

            return Ok(new { message = "Alert updated successfully.", memberAlertId = result });
        }

    }
}
