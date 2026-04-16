using AIInterviewCoach.Application.DTOs.AdminAuditLogs;
using AIInterviewCoach.Application.DTOs.Problems;
using AIInterviewCoach.Application.Interfaces.Services;
using AIInterviewCoach.Domain.Entities;
using AIInterviewCoach.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AIInterviewCoach.Application.Services
{
    public class ProblemService : IProblemService
    {
        private readonly IAppDbContext _dbContext;
        private readonly IAdminAuditService _adminAuditService;

        public ProblemService(IAppDbContext dbContext, IAdminAuditService adminAuditService)
        {
            _dbContext = dbContext;
            _adminAuditService = adminAuditService;
        }

        public async Task<IEnumerable<ProblemSummaryResponseDto>> GetAllProblemsAsync(Guid currentUserId, UserRole currentUserRole)
        {
            var query = _dbContext.Problems
                .AsNoTracking();

            if (currentUserRole != UserRole.Admin)
            {
                query = currentUserRole == UserRole.Candidate
                    ? query.Where(p => p.IsPublic)
                    : query.Where(p => p.IsPublic || p.CreatedByUserId == currentUserId);
            }

            return await query
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new ProblemSummaryResponseDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Description = p.Description,
                    Difficulty = p.Difficulty,
                    Topic = p.Topic,
                    ConstraintsText = p.ConstraintsText,
                    ExampleInput = p.ExampleInput,
                    ExampleOutput = p.ExampleOutput,
                    ExecutionMode = p.ExecutionMode,
                    IsPublic = p.IsPublic,
                    CreatedByUserId = p.CreatedByUserId,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt
                })
                .ToListAsync();
        }

        public async Task<ProblemResponseDto> GetProblemByIdAsync(Guid id, Guid currentUserId, UserRole currentUserRole)
        {
            var problem = await _dbContext.Problems
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (problem is null)
                throw new KeyNotFoundException("Problem not found.");

            EnsureProblemAccessible(problem, currentUserId, currentUserRole);

            return MapProblemToDto(problem);
        }

        public async Task<ProblemResponseDto> CreateProblemAsync(Guid userId, CreateProblemRequestDto createRequest)
        {
            if (!Enum.TryParse<DifficultyLevel>(createRequest.Difficulty, true, out var parsedDifficulty))
            {
                parsedDifficulty = DifficultyLevel.Easy;
            }

            var normalizedExecutionMode = NormalizeExecutionMode(createRequest.ExecutionMode);
            EnsureProblemExecutionConfiguration(
                normalizedExecutionMode,
                createRequest.CsharpStarterCode,
                createRequest.PythonStarterCode,
                createRequest.CppStarterCode,
                createRequest.CsharpHarnessTemplate,
                createRequest.PythonHarnessTemplate,
                createRequest.CppHarnessTemplate);

            var problem = new Problem
            {
                Id = Guid.NewGuid(),
                Title = createRequest.Title.Trim(),
                Description = createRequest.Description.Trim(),
                Difficulty = parsedDifficulty.ToString(),
                Topic = createRequest.Topic.Trim(),
                ConstraintsText = createRequest.ConstraintsText.Trim(),
                ExampleInput = createRequest.ExampleInput.Trim(),
                ExampleOutput = createRequest.ExampleOutput.Trim(),
                ExecutionMode = normalizedExecutionMode,
                CsharpStarterCode = createRequest.CsharpStarterCode.Trim(),
                PythonStarterCode = createRequest.PythonStarterCode.Trim(),
                CppStarterCode = createRequest.CppStarterCode.Trim(),
                CsharpHarnessTemplate = createRequest.CsharpHarnessTemplate.Trim(),
                PythonHarnessTemplate = createRequest.PythonHarnessTemplate.Trim(),
                CppHarnessTemplate = createRequest.CppHarnessTemplate.Trim(),
                IsPublic = createRequest.IsPublic,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.Problems.Add(problem);
            await _adminAuditService.RecordAsync(new AdminAuditLogWriteRequestDto
            {
                AdminUserId = userId,
                ActionType = "problem.created",
                TargetType = "problem",
                TargetId = problem.Id,
                TargetDisplayName = problem.Title,
                Summary = $"Created problem \"{problem.Title}\".",
                Details = new Dictionary<string, string>
                {
                    ["difficulty"] = problem.Difficulty,
                    ["topic"] = problem.Topic,
                    ["executionMode"] = problem.ExecutionMode,
                    ["visibility"] = problem.IsPublic ? "public" : "private"
                }
            });
            await _dbContext.SaveChangesAsync();

            return MapProblemToDto(problem);
        }

        public async Task<ProblemResponseDto?> UpdateProblemAsync(Guid id, Guid userId, UpdateProblemRequestDto updateRequest)
        {
            var problem = await _dbContext.Problems
                .FirstOrDefaultAsync(p => p.Id == id);

            if (problem is null)
                return null;

            if (problem.CreatedByUserId != userId)
                throw new UnauthorizedAccessException("You can only update your own problems.");

            if (!Enum.TryParse<DifficultyLevel>(updateRequest.Difficulty, true, out var parsedDifficulty))
            {
                parsedDifficulty = DifficultyLevel.Easy;
            }

            var previousTitle = problem.Title;
            var previousDifficulty = problem.Difficulty;
            var previousTopic = problem.Topic;
            var previousExecutionMode = problem.ExecutionMode;
            var previousVisibility = problem.IsPublic;

            var normalizedExecutionMode = NormalizeExecutionMode(updateRequest.ExecutionMode);
            EnsureProblemExecutionConfiguration(
                normalizedExecutionMode,
                updateRequest.CsharpStarterCode,
                updateRequest.PythonStarterCode,
                updateRequest.CppStarterCode,
                updateRequest.CsharpHarnessTemplate,
                updateRequest.PythonHarnessTemplate,
                updateRequest.CppHarnessTemplate);

            problem.Title = updateRequest.Title.Trim();
            problem.Description = updateRequest.Description.Trim();
            problem.Difficulty = parsedDifficulty.ToString();
            problem.Topic = updateRequest.Topic.Trim();
            problem.ConstraintsText = updateRequest.ConstraintsText.Trim();
            problem.ExampleInput = updateRequest.ExampleInput.Trim();
            problem.ExampleOutput = updateRequest.ExampleOutput.Trim();
            problem.ExecutionMode = normalizedExecutionMode;
            problem.CsharpStarterCode = updateRequest.CsharpStarterCode.Trim();
            problem.PythonStarterCode = updateRequest.PythonStarterCode.Trim();
            problem.CppStarterCode = updateRequest.CppStarterCode.Trim();
            problem.CsharpHarnessTemplate = updateRequest.CsharpHarnessTemplate.Trim();
            problem.PythonHarnessTemplate = updateRequest.PythonHarnessTemplate.Trim();
            problem.CppHarnessTemplate = updateRequest.CppHarnessTemplate.Trim();
            problem.IsPublic = updateRequest.IsPublic;
            problem.UpdatedAt = DateTime.UtcNow;

            await _adminAuditService.RecordAsync(new AdminAuditLogWriteRequestDto
            {
                AdminUserId = userId,
                ActionType = "problem.updated",
                TargetType = "problem",
                TargetId = problem.Id,
                TargetDisplayName = problem.Title,
                Summary = $"Updated problem \"{problem.Title}\".",
                Details = new Dictionary<string, string>
                {
                    ["previousTitle"] = previousTitle,
                    ["currentTitle"] = problem.Title,
                    ["previousDifficulty"] = previousDifficulty,
                    ["currentDifficulty"] = problem.Difficulty,
                    ["previousTopic"] = previousTopic,
                    ["currentTopic"] = problem.Topic,
                    ["previousExecutionMode"] = previousExecutionMode,
                    ["currentExecutionMode"] = problem.ExecutionMode,
                    ["previousVisibility"] = previousVisibility ? "public" : "private",
                    ["currentVisibility"] = problem.IsPublic ? "public" : "private"
                }
            });
            await _dbContext.SaveChangesAsync();

            return MapProblemToDto(problem);
        }

        public async Task<bool> DeleteProblemAsync(Guid id, Guid userId, bool isAdmin)
        {
            var problem = await _dbContext.Problems
                .Include(p => p.TestCases)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (problem is null)
                return false;

            if (!isAdmin && problem.CreatedByUserId != userId)
                throw new UnauthorizedAccessException("You can only delete your own problems.");

            var isAttachedToInterview = await _dbContext.InterviewProblems
                .AnyAsync(interviewProblem => interviewProblem.ProblemId == id);

            if (isAttachedToInterview)
                throw new InvalidOperationException("This problem is already attached to an interview. Remove the interview first, or use the catalog replacement tool to reset the workspace.");

            if (isAdmin)
            {
                await _adminAuditService.RecordAsync(new AdminAuditLogWriteRequestDto
                {
                    AdminUserId = userId,
                    ActionType = "problem.deleted",
                    TargetType = "problem",
                    TargetId = problem.Id,
                    TargetDisplayName = problem.Title,
                    Summary = $"Deleted problem \"{problem.Title}\".",
                    Details = new Dictionary<string, string>
                    {
                        ["difficulty"] = problem.Difficulty,
                        ["topic"] = problem.Topic,
                        ["executionMode"] = problem.ExecutionMode,
                        ["visibility"] = problem.IsPublic ? "public" : "private",
                        ["testCaseCount"] = problem.TestCases.Count.ToString()
                    }
                });
            }

            _dbContext.TestCases.RemoveRange(problem.TestCases);
            _dbContext.Problems.Remove(problem);

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<ReplaceProblemCatalogResponseDto> ReplaceCatalogWithStarterSetAsync(Guid userId)
        {
            var deletedProblemCount = await _dbContext.Problems.CountAsync();
            var deletedInterviewCount = await _dbContext.Interviews.CountAsync();
            var deletedSubmissionCount = await _dbContext.Submissions.CountAsync();

            _dbContext.Submissions.RemoveRange(_dbContext.Submissions);
            _dbContext.InterviewSessions.RemoveRange(_dbContext.InterviewSessions);
            _dbContext.InterviewProblems.RemoveRange(_dbContext.InterviewProblems);
            _dbContext.Interviews.RemoveRange(_dbContext.Interviews);
            _dbContext.TestCases.RemoveRange(_dbContext.TestCases);
            _dbContext.Problems.RemoveRange(_dbContext.Problems);

            await _dbContext.SaveChangesAsync();

            var now = DateTime.UtcNow;
            var candidateStatistics = await _dbContext.CandidateStatistics.ToListAsync();
            foreach (var statistic in candidateStatistics)
            {
                statistic.ProblemsSolved = 0;
                statistic.TotalSubmissions = 0;
                statistic.AccuracyRate = 0;
                statistic.AverageExecutionTimeMs = 0;
                statistic.UpdatedAt = now;
            }

            var starterProblemSeeds = StarterProblemCatalogFactory.Build(userId);
            _dbContext.Problems.AddRange(starterProblemSeeds.Select(seed => seed.Problem));
            _dbContext.TestCases.AddRange(starterProblemSeeds.SelectMany(seed => seed.TestCases));

            var result = new ReplaceProblemCatalogResponseDto
            {
                DeletedProblemCount = deletedProblemCount,
                DeletedInterviewCount = deletedInterviewCount,
                DeletedSubmissionCount = deletedSubmissionCount,
                CreatedProblemCount = starterProblemSeeds.Count,
                CreatedProblemTitles = starterProblemSeeds
                    .Select(seed => seed.Problem.Title)
                    .ToArray()
            };

            await _adminAuditService.RecordAsync(new AdminAuditLogWriteRequestDto
            {
                AdminUserId = userId,
                ActionType = "catalog.replaced",
                TargetType = "problem_catalog",
                Summary = "Replaced the problem catalog with the starter set.",
                Details = new Dictionary<string, string>
                {
                    ["deletedProblemCount"] = result.DeletedProblemCount.ToString(),
                    ["deletedInterviewCount"] = result.DeletedInterviewCount.ToString(),
                    ["deletedSubmissionCount"] = result.DeletedSubmissionCount.ToString(),
                    ["createdProblemCount"] = result.CreatedProblemCount.ToString(),
                    ["createdProblemTitles"] = string.Join(", ", result.CreatedProblemTitles)
                }
            });

            await _dbContext.SaveChangesAsync();

            return result;
        }

        public async Task<TestCaseResponseDto> AddTestCaseAsync(
            Guid problemId,
            Guid currentUserId,
            bool isAdmin,
            CreateTestCaseRequestDto createTestCaseRequest)
        {
            var problem = await _dbContext.Problems
                .FirstOrDefaultAsync(p => p.Id == problemId);

            if (problem is null)
                throw new KeyNotFoundException("Problem not found.");

            EnsureProblemOwnership(problem, currentUserId, isAdmin);

            var testCase = new TestCase
            {
                Id = Guid.NewGuid(),
                ProblemId = problemId,
                Input = createTestCaseRequest.Input,
                ExpectedOutput = createTestCaseRequest.ExpectedOutput,
                IsHidden = createTestCaseRequest.IsHidden,
                OrderIndex = createTestCaseRequest.OrderIndex
            };

            _dbContext.TestCases.Add(testCase);
            if (isAdmin)
            {
                await _adminAuditService.RecordAsync(new AdminAuditLogWriteRequestDto
                {
                    AdminUserId = currentUserId,
                    ActionType = "testcase.created",
                    TargetType = "problem_test_case",
                    TargetId = testCase.Id,
                    TargetDisplayName = problem.Title,
                    Summary = $"Added test case #{testCase.OrderIndex} to \"{problem.Title}\".",
                    Details = new Dictionary<string, string>
                    {
                        ["problemId"] = problem.Id.ToString(),
                        ["problemTitle"] = problem.Title,
                        ["isHidden"] = testCase.IsHidden ? "true" : "false",
                        ["orderIndex"] = testCase.OrderIndex.ToString()
                    }
                });
            }
            await _dbContext.SaveChangesAsync();

            return new TestCaseResponseDto
            {
                Id = testCase.Id,
                ProblemId = testCase.ProblemId,
                Input = testCase.Input,
                ExpectedOutput = testCase.ExpectedOutput,
                IsHidden = testCase.IsHidden,
                OrderIndex = testCase.OrderIndex
            };
        }

        public async Task<IEnumerable<TestCaseResponseDto>> GetTestCasesAsync(
            Guid problemId,
            Guid currentUserId,
            bool isAdmin,
            bool includeHidden = false)
        {
            var problem = await _dbContext.Problems
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == problemId);

            if (problem is null)
                throw new KeyNotFoundException("Problem not found.");

            EnsureProblemOwnership(problem, currentUserId, isAdmin);

            var query = _dbContext.TestCases
                .AsNoTracking()
                .Where(t => t.ProblemId == problemId);

            if (!includeHidden)
            {
                query = query.Where(t => !t.IsHidden);
            }

            return await query
                .OrderBy(t => t.OrderIndex)
                .Select(t => new TestCaseResponseDto
                {
                    Id = t.Id,
                    ProblemId = t.ProblemId,
                    Input = t.Input,
                    ExpectedOutput = t.ExpectedOutput,
                    IsHidden = t.IsHidden,
                    OrderIndex = t.OrderIndex
                })
                    .ToListAsync();
        }

        private static void EnsureProblemAccessible(Problem problem, Guid currentUserId, UserRole currentUserRole)
        {
            if (currentUserRole == UserRole.Admin)
                return;

            if (currentUserRole == UserRole.Candidate && !problem.IsPublic)
                throw new UnauthorizedAccessException("You do not have access to this problem.");

            if (currentUserRole == UserRole.Interviewer &&
                problem.CreatedByUserId != currentUserId &&
                !problem.IsPublic)
            {
                throw new UnauthorizedAccessException("You do not have access to this problem.");
            }
        }

        private static void EnsureProblemOwnership(Problem problem, Guid currentUserId, bool isAdmin)
        {
            if (!isAdmin && problem.CreatedByUserId != currentUserId)
                throw new UnauthorizedAccessException("You can only manage test cases for your own problems.");
        }

        private static ProblemResponseDto MapProblemToDto(Problem problem)
        {
            return new ProblemResponseDto
            {
                Id = problem.Id,
                Title = problem.Title,
                Description = problem.Description,
                Difficulty = problem.Difficulty.ToString(),
                Topic = problem.Topic,
                ConstraintsText = problem.ConstraintsText,
                ExampleInput = problem.ExampleInput,
                ExampleOutput = problem.ExampleOutput,
                ExecutionMode = problem.ExecutionMode,
                CsharpStarterCode = problem.CsharpStarterCode,
                PythonStarterCode = problem.PythonStarterCode,
                CppStarterCode = problem.CppStarterCode,
                IsPublic = problem.IsPublic,
                CreatedByUserId = problem.CreatedByUserId,
                CreatedAt = problem.CreatedAt,
                UpdatedAt = problem.UpdatedAt
            };
        }

        private static string NormalizeExecutionMode(string executionMode)
        {
            return executionMode.Trim().ToLowerInvariant() switch
            {
                ProblemExecutionModes.FunctionSignature => ProblemExecutionModes.FunctionSignature,
                _ => ProblemExecutionModes.Stdin
            };
        }

        private static void EnsureProblemExecutionConfiguration(
            string executionMode,
            string csharpStarterCode,
            string pythonStarterCode,
            string cppStarterCode,
            string csharpHarnessTemplate,
            string pythonHarnessTemplate,
            string cppHarnessTemplate)
        {
            if (executionMode != ProblemExecutionModes.FunctionSignature)
                return;

            var hasMissingStarter =
                string.IsNullOrWhiteSpace(csharpStarterCode) ||
                string.IsNullOrWhiteSpace(pythonStarterCode) ||
                string.IsNullOrWhiteSpace(cppStarterCode);
            var hasMissingHarness =
                string.IsNullOrWhiteSpace(csharpHarnessTemplate) ||
                string.IsNullOrWhiteSpace(pythonHarnessTemplate) ||
                string.IsNullOrWhiteSpace(cppHarnessTemplate);

            if (hasMissingStarter || hasMissingHarness)
                throw new InvalidOperationException("Function-signature problems require starter code and hidden harness templates for C#, Python, and C++.");

            EnsureHarnessTemplate(csharpHarnessTemplate, "C#");
            EnsureHarnessTemplate(pythonHarnessTemplate, "Python");
            EnsureHarnessTemplate(cppHarnessTemplate, "C++");
        }

        private static void EnsureHarnessTemplate(string template, string languageLabel)
        {
            if (!template.Contains("{{candidate_code}}", StringComparison.Ordinal))
                throw new InvalidOperationException($"{languageLabel} harness templates must contain the {{candidate_code}} placeholder.");
        }
    }
}
