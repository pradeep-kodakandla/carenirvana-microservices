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
    private readonly IRecentlyAccessed _recentlyAccessedService;
    public UserController(IUserService userService, IConfiguration configuration, IRecentlyAccessed recentlyAccessedService)
    {
        _userService = userService;
        _configuration = configuration;
        _recentlyAccessedService = recentlyAccessedService;
    }

    [HttpPost("authenticate")]
    public IActionResult Authenticate([FromBody] Login loginParam)
    {
        if (loginParam == null || string.IsNullOrEmpty(loginParam.UserName) || string.IsNullOrEmpty(loginParam.Password))
        {
            return BadRequest(new { error = "Invalid request payload" });
        }

        // Capture server-side context (more trustworthy than client-reported)
        var serverIp = GetClientIpAddress();
        var userAgent = Request.Headers["User-Agent"].ToString();

        // Build a context object for the service layer
        var loginContext = new LoginAttemptContext
        {
            ClientReportedIp = loginParam.IpAddress,
            ServerObservedIp = serverIp,
            Latitude = loginParam.Latitude,
            Longitude = loginParam.Longitude,
            LocationAccuracy = loginParam.LocationAccuracy,
            UserAgent = userAgent,
            AttemptedAt = DateTime.UtcNow
        };

        var user = _userService.Authenticate(loginParam.UserName, loginParam.Password, loginContext);

        if (user == null)
        {
            // Log failed attempt with context (helpful for security auditing)
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
            Message = "Login successful!",
            UserId = user.UserId
        };
        return Ok(response);
    }

    private string GetClientIpAddress()
    {
        // X-Forwarded-For if behind a proxy/load balancer (e.g., nginx, Azure, AWS ELB)
        var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            // X-Forwarded-For can be a comma-separated list; the first entry is the original client
            return forwardedFor.Split(',')[0].Trim();
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    [HttpGet("alluser")]
    public async Task<ActionResult<List<SecurityUser>>> GetAllUsers()
    {
        var users = await _userService.GetUserDetails();
        return Ok(users);
    }

    // 📦 Get full user data with details by ID
    [HttpGet("{id}")]
    public async Task<ActionResult<SecurityUser>> GetUserById(int id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user == null) return NotFound();
        return Ok(user);
    }

    // ➕ Add new user
    [HttpPost]
    public async Task<ActionResult<int>> AddUser([FromBody] SecurityUser user)
    {
        var id = await _userService.AddAsync(user);
        return CreatedAtAction(nameof(GetUserById), new { id }, id);
    }

    // ✏️ Update user
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] SecurityUser user)
    {
        if (id != user.UserId)
            return BadRequest("User ID mismatch");

        await _userService.UpdateAsync(user);
        return NoContent();
    }

    // ❌ Delete user
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id, [FromQuery] int deletedBy)
    {
        await _userService.DeleteAsync(id, deletedBy);
        return NoContent();
    }

    // 📜 Get recently accessed items for a user
    [HttpGet("{userId}/recentlyaccessed")]
    public async Task<ActionResult<IEnumerable<RecentlyAccessedView>>> GetRecentlyAccessed(
        int userId,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        [FromQuery] int limit = 100,
        [FromQuery] int offset = 0)
    {
        var items = await _recentlyAccessedService.GetByUserAsync(userId, fromUtc, toUtc, limit, offset);
        return Ok(items);
    }

    [HttpPost("{userId}/recentlyaccessed")]
    public async Task<ActionResult<int>> AddRecentlyAccessed(int userId, [FromBody] RecentlyAccessed item)
    {
        if (item.UserId != userId)
            return BadRequest("User ID mismatch");
        var id = await _recentlyAccessedService.InsertAsync(item);
        return CreatedAtAction(nameof(GetRecentlyAccessed), new { userId }, id);
    }
    [HttpGet("{userId}/recentlyaccessed/counts")]
    public async Task<ActionResult<Last24hCounts>> GetRecentlyAccessedCounts(int userId)
    {
        var counts = await _recentlyAccessedService.GetLast24hCountsAsync(userId);
        return Ok(counts);
    }
}