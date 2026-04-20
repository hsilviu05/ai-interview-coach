using AIInterviewCoach.Application.DTOs.Auth;
using AIInterviewCoach.Application.Interfaces.Services;
using AIInterviewCoach.API.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using AIInterviewCoach.API.RateLimiting;
using Microsoft.AspNetCore.Http;

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
        [ProducesResponseType(typeof(AuthSessionResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            var result = await _authService.RegisterAsync(request);
            return Ok(ToResponse(result));
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitingPolicies.AuthLogin)]
        [ProducesResponseType(typeof(AuthSessionResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            var result = await _authService.LoginAsync(request);
            AppendAuthCookie(result.Token);
            return Ok(ToResponse(result));
        }

        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(AuthSessionResponseDto), StatusCodes.Status200OK)]
        public IActionResult Me()
        {
            return Ok(new AuthSessionResponseDto
            {
                FullName = User.Identity?.Name ?? string.Empty,
                Email = User.Claims.FirstOrDefault(x => x.Type.Contains("email"))?.Value ?? string.Empty,
                Role = User.Claims.FirstOrDefault(x => x.Type.Contains("role"))?.Value ?? string.Empty
            });
        }

        [HttpPost("logout")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public IActionResult Logout()
        {
            Response.Cookies.Delete(AuthCookieDefaults.CookieName, BuildCookieOptions());
            return NoContent();
        }

        private void AppendAuthCookie(string token)
        {
            Response.Cookies.Append(
                AuthCookieDefaults.CookieName,
                token,
                BuildCookieOptions(DateTimeOffset.UtcNow.Add(AuthCookieDefaults.Lifetime)));
        }

        private CookieOptions BuildCookieOptions(DateTimeOffset? expires = null)
        {
            return new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                IsEssential = true,
                Expires = expires
            };
        }

        private static AuthSessionResponseDto ToResponse(AuthResponseDto result)
        {
            return new AuthSessionResponseDto
            {
                FullName = result.FullName,
                Email = result.Email,
                Role = result.Role
            };
        }
    }
}
