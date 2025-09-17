using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Application.Services;
using CareNirvana.Service.Domain.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

[Route("api/[controller]")]
[ApiController]
public class EnrollmentController : ControllerBase
{
    private readonly IMemberEnrollmentRepository _memberEnrollmentService;

    public EnrollmentController(IMemberEnrollmentRepository memberEnrollmentService)
    {
        _memberEnrollmentService = memberEnrollmentService;
    }
    [HttpGet("{memberdetailsId}")]  
    public async Task<IActionResult> GetMemberEnrollment(int memberdetailsId)
    {
        var result = await _memberEnrollmentService.GetMemberEnrollment(memberdetailsId);
        if (result == null || result.Count == 0)
            return NotFound(new { message = "No enrollment data found for this member." });
        return Ok(result);
    }
}

