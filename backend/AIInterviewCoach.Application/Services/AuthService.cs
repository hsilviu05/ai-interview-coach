using AIInterviewCoach.Application.DTOs.Auth;
using AIInterviewCoach.Application.Interfaces.Services;
using AIInterviewCoach.Domain.Entities;
using AIInterviewCoach.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AIInterviewCoach.Application.Services;

public class AuthService : IAuthService
{
    private readonly IAppDbContext _dbContext;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(
        IAppDbContext dbContext,
        IPasswordHasherService passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        var normalizedEmail = request.Email.Trim().ToLower();

        var existingUser = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Email == normalizedEmail);

        if (existingUser is not null)
            throw new InvalidOperationException("Email is already in use.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName,
            Email = normalizedEmail,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            UserRole = UserRole.Candidate,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Users.Add(user);

        if (user.UserRole == UserRole.Candidate)
        {
            _dbContext.CandidateStatistics.Add(new CandidateStatistic
            {
                Id = Guid.NewGuid(),
                CandidateId = user.Id,
                ProblemsSolved = 0,
                TotalSubmissions = 0,
                AccuracyRate = 0,
                AverageExecutionTimeMs = 0,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await _dbContext.SaveChangesAsync();

        var token = _jwtTokenService.GenerateToken(user);

        return new AuthResponseDto
        {
            Token = token,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.UserRole.ToString()
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Email == request.Email.Trim().ToLower());

        if (user is null)
            throw new InvalidOperationException("Invalid email or password.");

        var isPasswordValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);

        if (!isPasswordValid)
            throw new InvalidOperationException("Invalid email or password.");

        var token = _jwtTokenService.GenerateToken(user);

        return new AuthResponseDto
        {
            Token = token,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.UserRole.ToString()
        };
    }
}
