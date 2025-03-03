using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Threading.Tasks;
using CareNirvana.Service.Application.Interfaces;

namespace CareNirvana.Service.API.Controllers.Admin
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConfigAdminController : ControllerBase
    {
        private readonly IConfigAdminService _configAdminService;

        public ConfigAdminController(IConfigAdminService configAdminService)
        {
            _configAdminService = configAdminService;
        }

        [HttpGet("{module}/{section}")]
        public async Task<IActionResult> GetSectionData(string module, string section)
        {
            var result = await _configAdminService.GetSectionData(module, section);
            if (result == null)
                return NotFound(new { message = "No data found for this section" });
            return Ok(result);
        }

        [HttpPost("{module}/{section}")]
        public async Task<IActionResult> AddEntry(string module, string section, [FromBody] JsonElement newEntry)
        {
            var result = await _configAdminService.AddEntry(module, section, newEntry);
            return Ok(result);
        }

        [HttpPut("{module}/{section}/{id}")]
        public async Task<IActionResult> UpdateEntry(string module, string section, string id, [FromBody] JsonElement updatedEntry)
        {
            var result = await _configAdminService.UpdateEntry(module, section, id, updatedEntry);
            if (result == null)
                return NotFound(new { message = "ID not found" });
            return Ok(result);
        }

        [HttpPatch("{module}/{section}/{id}")]
        public async Task<IActionResult> DeleteEntry(string module, string section, string id, [FromBody] JsonElement deleteInfo)
        {
            var result = await _configAdminService.DeleteEntry(module, section, id, deleteInfo);
            if (!result)
                return NotFound(new { message = "Record not found" });
            return Ok(new { message = "Record deleted successfully." });
        }
    }
}

