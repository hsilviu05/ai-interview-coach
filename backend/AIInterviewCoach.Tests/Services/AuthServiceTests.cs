using AIInterviewCoach.Application.DTOs.Auth;
using AIInterviewCoach.Application.Interfaces.Services;
using AIInterviewCoach.Application.Services;
using AIInterviewCoach.Domain.Entities;
using AIInterviewCoach.Domain.Enums;
using AIInterviewCoach.Infrastructure.Services;
using AIInterviewCoach.Tests.Common;

namespace AIInterviewCoach.Tests.Services
{
    public class AuthServiceTests
    {
        [Fact]
        public async Task RegisterAsync_ShouldAlwaysCreateCandidateAccounts()
        {
            using var db = TestDbContextFactory.CreateContext();

            var service = new AuthService(
                db,
                new PasswordHasherService(),
                new FakeJwtTokenService());

            var result = await service.RegisterAsync(new RegisterRequestDto
            {
                FullName = "New Interviewer",
                Email = "new-user@test.com",
                Password = "Password123!"
            });

            var storedUser = db.Users.Single(x => x.Email == "new-user@test.com");

            Assert.Equal(UserRole.Candidate, storedUser.UserRole);
            Assert.Equal("Candidate", result.Role);
            Assert.NotNull(db.CandidateStatistics.SingleOrDefault(x => x.CandidateId == storedUser.Id));
        }

        [Fact]
        public async Task LoginAsync_ShouldReturnInvalidOperation_WhenCredentialsAreWrong()
        {
            using var db = TestDbContextFactory.CreateContext();

            var passwordHasher = new PasswordHasherService();
            db.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                FullName = "Candidate",
                Email = "candidate@test.com",
                PasswordHash = passwordHasher.HashPassword("Password123!"),
                UserRole = UserRole.Candidate,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var service = new AuthService(
                db,
                passwordHasher,
                new FakeJwtTokenService());

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.LoginAsync(new LoginRequestDto
                {
                    Email = "candidate@test.com",
                    Password = "WrongPassword123!"
                }));
        }

        private sealed class FakeJwtTokenService : IJwtTokenService
        {
            public string GenerateToken(User user) => $"token-for-{user.Id}";
        }
    }
}
