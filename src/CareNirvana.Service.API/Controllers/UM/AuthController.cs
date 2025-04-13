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

        [HttpGet("fetch/{authclassId}")]
        public async Task<ActionResult<List<AuthTemplate>>> GetAuthTemplates(int authclassId)
        {
            var result = await _getAuthTemplatesQuery.ExecuteAsync(authclassId);
            return Ok(result);
        }

        [HttpGet("template/{id}")]
        public async Task<ActionResult<List<AuthTemplate>>> GetAuthTemplate(int id)
        {
            var result = await _getAuthTemplatesQuery.GetTemplate(id);
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

        [HttpPost("savetemplate")]
        public async Task<IActionResult> SaveAuthTemplate([FromBody] AuthTemplate authTemplate)
        {
            try
            {
                if (authTemplate == null || authTemplate.JsonContent == null || !authTemplate.JsonContent.Any() || authTemplate.TemplateName == null)
                {
                    return BadRequest("Invalid data received");
                }

                await _getAuthTemplatesQuery.ExecuteAsync(authTemplate);
                return Ok("Data saved successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving auth detail: {ex.Message}");
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }


        // New API: Get all AuthDetails by Member ID
        [HttpGet("member/{memberId}")]
        public async Task<ActionResult<List<AuthDetail>>> GetAllByMemberId(int memberId)
        {
            try
            {
                var result = await _saveAuthDetailCommand.GetAllAsync(memberId);
                if (result == null || result.Count == 0)
                {
                    return Ok(new List<AuthDetail>());  // Return an empty list instead of 404
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching auth details: {ex.Message}");
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }

        // New API: Get AuthDetails by Auth Number
        [HttpGet("auth/{authNumber}")]
        public async Task<ActionResult<List<AuthDetail>>> GetAuthData(string authNumber)
        {
            try
            {
                var result = await _saveAuthDetailCommand.GetAuthData(authNumber);
                if (result == null || result.Count == 0)
                {
                    return Ok(new List<AuthDetail>());  // Return an empty list instead of 404
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching auth details: {ex.Message}");
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }
    }
}


