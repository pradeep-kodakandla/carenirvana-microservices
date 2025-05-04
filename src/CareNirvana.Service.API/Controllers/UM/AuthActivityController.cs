using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Application.UseCases;
using CareNirvana.Service.Domain.Model;
using Microsoft.AspNetCore.Mvc;


namespace CareNirvana.Service.API.Controllers
{
    [Route("api/[controller]")]
    public class AuthActivityController : ControllerBase
    {
        private readonly IAuthActivityRepository _service;

        public AuthActivityController(IAuthActivityRepository service)
        {
            _service = service;
        }

        [HttpGet("authdetail/{authdetailid}")]
        public async Task<IActionResult> GetAll(int authdetailid) =>
            Ok(await _service.GetAllAsync(authdetailid));

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var result = await _service.GetByIdAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AuthActivity activity)
        {
            var result = await _service.InsertAsync(activity);
            return CreatedAtAction(nameof(Get), new { id = result.AuthActivityId }, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] AuthActivity activity)
        {
            if (id != activity.AuthActivityId) return BadRequest();
            var result = await _service.UpdateAsync(activity);
            return Ok(result);
        }
    }
}
