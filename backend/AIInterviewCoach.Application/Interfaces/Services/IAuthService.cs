using AIInterviewCoach.Application.DTOs.Auth;

namespace AIInterviewCoach.Application.Interfaces.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterRequestDto registerRequest);
        Task<AuthResponseDto> LoginAsync(LoginRequestDto loginRequest);
    }
}