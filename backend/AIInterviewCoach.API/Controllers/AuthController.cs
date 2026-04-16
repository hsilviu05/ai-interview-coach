using AIInterviewCoach.Application.DTOs.Auth;
using AIInterviewCoach.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using AIInterviewCoach.API.RateLimiting;

namespace AIInterviewCoach.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }
        [HttpPost("register")]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitingPolicies.AuthLogin)]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            var result = await _authService.RegisterAsync(request);
            return Ok(result);
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitingPolicies.AuthLogin)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            var result = await _authService.LoginAsync(request);
            return Ok(result);
        }

        [HttpGet("me")]
        [Authorize]
        public IActionResult Me()
        {
            return Ok(new
            {
                Name = User.Identity?.Name,
                Email = User.Claims.FirstOrDefault(x => x.Type.Contains("email"))?.Value,
                Role = User.Claims.FirstOrDefault(x => x.Type.Contains("role"))?.Value
            });
        }
    }
}
