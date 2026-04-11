using AIInterviewCoach.Domain.Entities;
using AIInterviewCoach.Domain.Enums;
using AIInterviewCoach.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace AIInterviewCoach.Infrastructure.Persistence
{
    public class DBSedder
    {
        private const string DemoPassword = "Password123!";

        public static async Task SeedAsync(AppDbContext context)
        {
            var passwordHasher = new PasswordHasherService();

            var recruiter = EnsureDemoUser(
                context,
                passwordHasher,
                "recruiter@test.com",
                "Recruiter Demo",
                UserRole.Interviewer);

            var candidate = EnsureDemoUser(
                context,
                passwordHasher,
                "candidate@test.com",
                "Candidate Demo",
                UserRole.Candidate);

            EnsureCandidateStatistic(context, candidate);
            BackfillLegacyProblemMetadata(context);

            await context.SaveChangesAsync();
        }

        private static User EnsureDemoUser(
            AppDbContext context,
            PasswordHasherService passwordHasher,
            string email,
            string fullName,
            UserRole role)
        {
            var existingUser = context.Users.FirstOrDefault(user => user.Email == email);
            if (existingUser is not null)
            {
                existingUser.FullName = fullName;
                existingUser.UserRole = role;
                existingUser.PasswordHash = passwordHasher.HashPassword(DemoPassword);
                existingUser.UpdatedAt = DateTime.UtcNow;
                return existingUser;
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                FullName = fullName,
                Email = email,
                PasswordHash = passwordHasher.HashPassword(DemoPassword),
                UserRole = role,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Users.Add(user);
            return user;
        }

        private static void EnsureCandidateStatistic(AppDbContext context, User candidate)
        {
            var existingStatistic = context.CandidateStatistics
                .FirstOrDefault(statistic => statistic.CandidateId == candidate.Id);

            if (existingStatistic is not null)
            {
                existingStatistic.UpdatedAt = DateTime.UtcNow;
                return;
            }

            context.CandidateStatistics.Add(new CandidateStatistic
            {
                Id = Guid.NewGuid(),
                CandidateId = candidate.Id,
                ProblemsSolved = 0,
                TotalSubmissions = 0,
                AccuracyRate = 0,
                AverageExecutionTimeMs = 0,
                UpdatedAt = DateTime.UtcNow
            });
        }

        private static void BackfillLegacyProblemMetadata(AppDbContext context)
        {
            var problems = context.Problems
                .Include(problem => problem.TestCases)
                .ToList();

            foreach (var problem in problems)
            {
                var firstVisibleTestCase = problem.TestCases
                    .Where(testCase => !testCase.IsHidden)
                    .OrderBy(testCase => testCase.OrderIndex)
                    .FirstOrDefault();
                var updated = false;

                if (string.IsNullOrWhiteSpace(problem.Description))
                {
                    problem.Description =
                        "No written description was provided for this problem yet. Use the title and sample cases below as the starting point.";
                    updated = true;
                }

                if (string.IsNullOrWhiteSpace(problem.Difficulty))
                {
                    problem.Difficulty = "Unspecified";
                    updated = true;
                }

                if (string.IsNullOrWhiteSpace(problem.Topic))
                {
                    problem.Topic = "General";
                    updated = true;
                }

                if (string.IsNullOrWhiteSpace(problem.ConstraintsText))
                {
                    problem.ConstraintsText = "No additional constraints were provided.";
                    updated = true;
                }

                if (string.IsNullOrWhiteSpace(problem.ExampleInput) && firstVisibleTestCase is not null)
                {
                    problem.ExampleInput = firstVisibleTestCase.Input;
                    updated = true;
                }

                if (string.IsNullOrWhiteSpace(problem.ExampleOutput) && firstVisibleTestCase is not null)
                {
                    problem.ExampleOutput = firstVisibleTestCase.ExpectedOutput;
                    updated = true;
                }

                if (updated)
                    problem.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}
