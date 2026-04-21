using AIInterviewCoach.Application.DTOs.Problems;
using AIInterviewCoach.Application.Interfaces.Services;
using AIInterviewCoach.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AIInterviewCoach.Application.Services
{
    public class PracticeProblemHintRequestService : IPracticeProblemHintRequestService
    {
        private const int MinHintLevel = 1;
        private const int MaxHintLevel = 3;

        private readonly IAppDbContext _dbContext;
        private readonly IProblemHintService _problemHintService;

        public PracticeProblemHintRequestService(
            IAppDbContext dbContext,
            IProblemHintService problemHintService)
        {
            _dbContext = dbContext;
            _problemHintService = problemHintService;
        }

        public async Task<ProblemHintResponseDto> GeneratePracticeHintAsync(
            Guid problemId,
            Guid currentUserId,
            UserRole currentUserRole,
            ProblemHintRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (request.Level < MinHintLevel || request.Level > MaxHintLevel)
            {
                throw new InvalidOperationException("Hints are available only for levels 1 through 3.");
            }

            var problem = await _dbContext.Problems
                .AsNoTracking()
                .FirstOrDefaultAsync(problem => problem.Id == problemId, cancellationToken);

            if (problem is null)
            {
                throw new KeyNotFoundException("Problem not found.");
            }

            ProblemAccessAuthorization.EnsureProblemAccessible(
                problem,
                currentUserId,
                currentUserRole);

            var context = new ProblemHintContextDto
            {
                Level = request.Level,
                ProblemTitle = problem.Title,
                ProblemDescription = problem.Description,
                Difficulty = problem.Difficulty,
                Topic = problem.Topic,
                ConstraintsText = problem.ConstraintsText,
                ExampleInput = problem.ExampleInput,
                ExampleOutput = problem.ExampleOutput,
                Language = request.Language.Trim(),
                SourceCode = request.SourceCode
            };

            return await _problemHintService.GenerateHintAsync(context, cancellationToken);
        }
    }
}
