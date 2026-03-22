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
        public ProblemService(IAppDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<IEnumerable<ProblemResponseDto>> GetAllProblemsAsync()
        {
            return await _dbContext.Problems
                .AsNoTracking()
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new ProblemResponseDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Description = p.Description,
                    Difficulty = p.Difficulty,
                    Topic = p.Topic,
                    ConstraintsText = p.ConstraintsText,
                    ExampleInput = p.ExampleInput,
                    ExampleOutput = p.ExampleOutput,
                    CreatedByUserId = p.CreatedByUserId,
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync();
        }
        public async Task<ProblemResponseDto> GetProblemByIdAsync(Guid id)
        {
            var problem = await _dbContext.Problems
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (problem == null)
                throw new KeyNotFoundException("Problem not found.");

            return MapProblemToDto(problem);
        }
        public async Task<ProblemResponseDto> CreateProblemAsync(Guid userId, CreateProblemRequestDto createRequest)
        {
            if (!Enum.TryParse<DifficultyLevel>(createRequest.Difficulty, true, out var parsedDifficulty))
            {
                parsedDifficulty = DifficultyLevel.Easy;
            }

            var problem = new Problem
            {
                Id = Guid.NewGuid(),
                Title = createRequest.Title,
                Description = createRequest.Description,
                Difficulty = parsedDifficulty.ToString(),
                Topic = createRequest.Topic,
                ConstraintsText = createRequest.ConstraintsText,
                ExampleInput = createRequest.ExampleInput,
                ExampleOutput = createRequest.ExampleOutput,
                IsPublic = createRequest.IsPublic,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.Problems.Add(problem);
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

            problem.Title = updateRequest.Title.Trim();
            problem.Description = updateRequest.Description.Trim();
            problem.Difficulty = parsedDifficulty.ToString();
            problem.Topic = updateRequest.Topic.Trim();
            problem.ConstraintsText = updateRequest.ConstraintsText.Trim();
            problem.ExampleInput = updateRequest.ExampleInput.Trim();
            problem.ExampleOutput = updateRequest.ExampleOutput.Trim();
            problem.IsPublic = updateRequest.IsPublic;
            problem.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            return MapProblemToDto(problem);
        }
        public async Task<bool> DeleteProblemAsync(Guid id, Guid userId)
        {
            var problem = await _dbContext.Problems
            .Include(p => p.TestCases)
            .FirstOrDefaultAsync(p => p.Id == id);

            if (problem is null)
                return false;

            if (problem.CreatedByUserId != userId)
                throw new UnauthorizedAccessException("You can only delete your own problems.");

            _dbContext.TestCases.RemoveRange(problem.TestCases);
            _dbContext.Problems.Remove(problem);

            await _dbContext.SaveChangesAsync();
            return true;
        }
        public async Task<TestCaseResponseDto> AddTestCaseAsync(Guid problemId, CreateTestCaseRequestDto createTestCaseRequest)
        {
            var problemExists = await _dbContext.Problems
            .AnyAsync(p => p.Id == problemId);

            if (!problemExists)
                throw new KeyNotFoundException("Problem not found.");

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
        public async Task<IEnumerable<TestCaseResponseDto>> GetTestCasesAsync(Guid problemId, bool includeHidden = false)
        {
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
                IsPublic = problem.IsPublic,
                CreatedByUserId = problem.CreatedByUserId,
                CreatedAt = problem.CreatedAt,
                UpdatedAt = problem.UpdatedAt
            };
        }
    }
}