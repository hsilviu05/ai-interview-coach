using AIInterviewCoach.Application.DTOs.Problems;
using AIInterviewCoach.Application.Interfaces.Services;

namespace AIInterviewCoach.Tests.Common
{
    public sealed class FakeProblemHintService : IProblemHintService
    {
        private readonly Func<ProblemHintContextDto, ProblemHintResponseDto> _generateHint;

        public FakeProblemHintService(
            Func<ProblemHintContextDto, ProblemHintResponseDto>? generateHint = null)
        {
            _generateHint = generateHint ?? (context => new ProblemHintResponseDto
            {
                Level = context.Level,
                Content = $"Hint {context.Level}\nStay close to the example.",
                Source = "OpenAI"
            });
        }

        public Task<ProblemHintResponseDto> GenerateHintAsync(
            ProblemHintContextDto context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_generateHint(context));
        }
    }
}
