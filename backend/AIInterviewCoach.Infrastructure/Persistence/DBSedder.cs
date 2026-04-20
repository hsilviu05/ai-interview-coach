using AIInterviewCoach.Application.Services;
using AIInterviewCoach.Application.DTOs.Problems;
using AIInterviewCoach.Application.Services.ProblemSignatures;
using AIInterviewCoach.Domain.Entities;
using AIInterviewCoach.Domain.Enums;
using AIInterviewCoach.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace AIInterviewCoach.Infrastructure.Persistence
{
    public class DBSedder
    {
        private const string DemoPassword = "Password123!";
        private const string IntegrationTestingEnvironment = "IntegrationTesting";

        public static async Task SeedAsync(
            AppDbContext context,
            IConfiguration configuration,
            IHostEnvironment environment)
        {
            EnsureSafeStartupConfiguration(configuration, environment);

            if (!IsDevelopmentLikeEnvironment(environment))
            {
                return;
            }

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

            EnsureConfiguredAdminUser(context, passwordHasher, configuration);
            EnsureCandidateStatistic(context, candidate);
            BackfillLegacyProblemMetadata(context);
            BackfillLegacyStarterTemplates(context);

            await context.SaveChangesAsync();
        }

        public static void EnsureSafeStartupConfiguration(
            IConfiguration configuration,
            IHostEnvironment environment)
        {
            if (IsDevelopmentLikeEnvironment(environment))
            {
                return;
            }

            var bootstrapAdminEmail = configuration["BootstrapAdmin:Email"]?.Trim();
            var bootstrapAdminPassword = configuration["BootstrapAdmin:Password"];
            var bootstrapAdminFullName = configuration["BootstrapAdmin:FullName"]?.Trim();

            if (!string.IsNullOrWhiteSpace(bootstrapAdminEmail) ||
                !string.IsNullOrWhiteSpace(bootstrapAdminPassword) ||
                !string.IsNullOrWhiteSpace(bootstrapAdminFullName))
            {
                throw new InvalidOperationException(
                    "BootstrapAdmin configuration is only supported in the Development environment.");
            }
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

        private static void EnsureConfiguredAdminUser(
            AppDbContext context,
            PasswordHasherService passwordHasher,
            IConfiguration configuration)
        {
            var email = configuration["BootstrapAdmin:Email"]?.Trim();
            var password = configuration["BootstrapAdmin:Password"];

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return;
            }

            var fullName = configuration["BootstrapAdmin:FullName"]?.Trim();
            if (string.IsNullOrWhiteSpace(fullName))
            {
                fullName = "Local Admin";
            }

            EnsureConfiguredUser(
                context,
                passwordHasher,
                email,
                fullName,
                password,
                UserRole.Admin);
        }

        private static User EnsureConfiguredUser(
            AppDbContext context,
            PasswordHasherService passwordHasher,
            string email,
            string fullName,
            string password,
            UserRole role)
        {
            var existingUser = context.Users.FirstOrDefault(user => user.Email == email);
            if (existingUser is not null)
            {
                existingUser.FullName = fullName;
                existingUser.UserRole = role;
                existingUser.PasswordHash = passwordHasher.HashPassword(password);
                existingUser.UpdatedAt = DateTime.UtcNow;
                return existingUser;
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                FullName = fullName,
                Email = email,
                PasswordHash = passwordHasher.HashPassword(password),
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

                if (string.IsNullOrWhiteSpace(problem.ExecutionMode))
                {
                    problem.ExecutionMode = ProblemExecutionModes.Stdin;
                    updated = true;
                }

                if (updated)
                    problem.UpdatedAt = DateTime.UtcNow;
            }
        }

        private static void BackfillLegacyStarterTemplates(AppDbContext context)
        {
            var sharedTemplatesByTitle = ProblemTemplateCatalog.GetCreateProblemTemplates()
                .ToDictionary(template => template.Title, StringComparer.Ordinal);

            foreach (var problem in context.Problems)
            {
                if (problem.ExecutionMode != ProblemExecutionModes.FunctionSignature)
                {
                    continue;
                }

                if (!sharedTemplatesByTitle.TryGetValue(problem.Title, out var template))
                {
                    continue;
                }

                var currentPythonStarter = problem.PythonStarterCode?.Trim() ?? string.Empty;
                if (!ShouldUpgradeLegacyFunctionStarter(problem.Title, currentPythonStarter))
                {
                    if (!ShouldUpgradeLegacyStarterMetadata(problem, template))
                    {
                        continue;
                    }
                }

                problem.CsharpStarterCode = template.CsharpStarterCode.Trim();
                problem.PythonStarterCode = template.PythonStarterCode.Trim();
                problem.CppStarterCode = template.CppStarterCode.Trim();
                problem.SignatureDefinitionJson = template.Signature is null
                    ? null
                    : ProblemSignatureSerializer.Serialize(
                        ProblemSignatureValidation.ValidateAndNormalize(template.Signature));

                if (template.Signature is not null)
                {
                    var generatedArtifacts = ProblemSignatureCodeGenerator.Generate(template.Signature);
                    problem.CsharpHarnessTemplate = generatedArtifacts.CsharpHarnessCode;
                    problem.PythonHarnessTemplate = generatedArtifacts.PythonHarnessCode;
                    problem.CppHarnessTemplate = generatedArtifacts.CppHarnessCode;
                }

                if (ShouldUpgradeLegacyStarterMetadata(problem, template))
                {
                    problem.Description = template.Description;
                    problem.Difficulty = template.Difficulty;
                    problem.Topic = template.Topic;
                    problem.ConstraintsText = template.ConstraintsText;
                    problem.ExampleInput = template.ExampleInput;
                    problem.ExampleOutput = template.ExampleOutput;
                }
                problem.UpdatedAt = DateTime.UtcNow;
            }
        }

        private static bool ShouldUpgradeLegacyStarterMetadata(Problem problem, ProblemTemplateResponseDto template)
        {
            if (problem.ExecutionMode != ProblemExecutionModes.FunctionSignature)
            {
                return false;
            }

            return !string.Equals(problem.Description?.Trim(), template.Description.Trim(), StringComparison.Ordinal) ||
                !string.Equals(problem.Difficulty?.Trim(), template.Difficulty.Trim(), StringComparison.Ordinal) ||
                !string.Equals(problem.Topic?.Trim(), template.Topic.Trim(), StringComparison.Ordinal) ||
                !string.Equals(problem.ConstraintsText?.Trim(), template.ConstraintsText.Trim(), StringComparison.Ordinal) ||
                !string.Equals(problem.ExampleInput?.Trim(), template.ExampleInput.Trim(), StringComparison.Ordinal) ||
                !string.Equals(problem.ExampleOutput?.Trim(), template.ExampleOutput.Trim(), StringComparison.Ordinal) ||
                !string.Equals(
                    problem.SignatureDefinitionJson?.Trim(),
                    template.Signature is null
                        ? null
                        : ProblemSignatureSerializer.Serialize(
                            ProblemSignatureValidation.ValidateAndNormalize(template.Signature)),
                    StringComparison.Ordinal);
        }

        private static bool ShouldUpgradeLegacyFunctionStarter(string title, string pythonStarterCode)
        {
            if (string.IsNullOrWhiteSpace(pythonStarterCode))
            {
                return true;
            }

            return GetLegacyPythonStarterByTitle(title) is { } legacyStarter &&
                string.Equals(
                    pythonStarterCode,
                    legacyStarter.Trim(),
                    StringComparison.Ordinal);
        }

        private static string? GetLegacyPythonStarterByTitle(string title)
        {
            return title switch
            {
                "Generic Function Problem" =>
                """
                class Solution:
                    def solve(self, raw_input: str) -> str:
                        
                """,
                "Two Sum" =>
                """
                from typing import List


                class Solution:
                    def twoSum(self, nums: List[int], target: int) -> List[int]:
                        
                """,
                "Valid Parentheses" =>
                """
                class Solution:
                    def isValid(self, s: str) -> bool:
                        
                """,
                "Merge Strings Alternately" =>
                """
                class Solution:
                    def mergeAlternately(self, word1: str, word2: str) -> str:
                        
                """,
                "Best Time to Buy and Sell Stock" =>
                """
                from typing import List


                class Solution:
                    def maxProfit(self, prices: List[int]) -> int:
                        
                """,
                _ => null
            };
        }

        private static bool IsDevelopmentLikeEnvironment(IHostEnvironment environment)
        {
            return environment.IsDevelopment() ||
                environment.IsEnvironment(IntegrationTestingEnvironment);
        }
    }
}
