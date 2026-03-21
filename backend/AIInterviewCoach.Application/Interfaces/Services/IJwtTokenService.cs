using AIInterviewCoach.Domain.Entities;

namespace AIInterviewCoach.Application.Interfaces.Services
{
    public interface IJwtTokenService
    {
        string GenerateToken(User user);
    }
}