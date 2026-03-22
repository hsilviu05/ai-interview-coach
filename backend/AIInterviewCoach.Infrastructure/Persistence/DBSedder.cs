using AIInterviewCoach.Domain.Entities;
using AIInterviewCoach.Domain.Enums;

namespace AIInterviewCoach.Infrastructure.Persistence
{
    public class DBSedder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            if (context.Users.Any())
                return;

            var recruiter = new User
            {
                Id = Guid.NewGuid(),
                FullName = "Recruiter Demo",
                Email = "recruiter@test.com",
                PasswordHash = "hashedpassword",
                UserRole = UserRole.Interviewer,
                CreatedAt = DateTime.UtcNow
            };

            var candidate = new User
            {
                Id = Guid.NewGuid(),
                FullName = "Candidate Demo",
                Email = "candidate@test.com",
                PasswordHash = "hashedpassword",
                UserRole = UserRole.Candidate,
                CreatedAt = DateTime.UtcNow
            };

            context.Users.AddRange(recruiter, candidate);

            await context.SaveChangesAsync();
        }
    }
}
