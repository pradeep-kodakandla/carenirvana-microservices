using CareNirvana.Service.Application.UseCases;
using CareNirvana.Service.Domain.Model;
using Microsoft.AspNetCore.Mvc;


namespace CareNirvana.Service.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly GetAuthTemplatesQuery _getAuthTemplatesQuery;
        private readonly SaveAuthDetailCommand _saveAuthDetailCommand;

        public AuthController(GetAuthTemplatesQuery getAuthTemplatesQuery, SaveAuthDetailCommand saveAuthDetailCommand)
        {
            _getAuthTemplatesQuery = getAuthTemplatesQuery;
            _saveAuthDetailCommand = saveAuthDetailCommand;
        }

        [HttpGet("fetch")]
        public async Task<ActionResult<List<AuthTemplate>>> GetAuthTemplates()
        {
            var result = await _getAuthTemplatesQuery.ExecuteAsync();
            return Ok(result);
        }

        [HttpPost("save")]
        public async Task<IActionResult> SaveAuthDetail([FromBody] AuthDetail authDetail)
        {
            try
            {
                if (authDetail == null || authDetail.Data == null || !authDetail.Data.Any() || authDetail.AuthNumber == null)
                {
                    return BadRequest("Invalid data received");
                }

                await _saveAuthDetailCommand.ExecuteAsync(authDetail);
                return Ok("Data saved successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving auth detail: {ex.Message}");
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }



    }
}

