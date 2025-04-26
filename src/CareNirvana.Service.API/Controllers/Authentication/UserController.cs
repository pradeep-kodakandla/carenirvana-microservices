using CareNirvana.Service.Application.Interfaces;
using CareNirvana.Service.Domain.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

[Route("api/[controller]")]
[ApiController]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IConfiguration _configuration;

    public UserController(IUserService userService, IConfiguration configuration)
    {
        _userService = userService;
        _configuration = configuration;
    }

    [HttpPost("authenticate")]
    public IActionResult Authenticate([FromBody] Login loginParam)
    {
        if (loginParam == null || string.IsNullOrEmpty(loginParam.UserName) || string.IsNullOrEmpty(loginParam.Password))
        {
            return BadRequest(new { error = "Invalid request payload" });
        }

        var user = _userService.Authenticate(loginParam.UserName, loginParam.Password);

        if (user == null)
        {
            return Unauthorized(new { error = "Username or password is incorrect" });
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Secret"]);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Role, "User")
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        var response = new
        {
            Token = tokenString,
            UserName = user.UserName,
            Message = "Login successful!"
        };

        return Ok(response); // Simplified return, CORS middleware will handle headers
    }

    [HttpGet("alluser")]
    public async Task<ActionResult<List<SecurityUser>>> GetAllUsers()
    {
        var users = await _userService.GetUserDetails();
        return Ok(users);
    }
}