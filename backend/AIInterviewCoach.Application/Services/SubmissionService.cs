using AIInterviewCoach.Application.DTOs.Submissions;
using AIInterviewCoach.Application.Interfaces.Services;
using AIInterviewCoach.Domain.Entities;
using AIInterviewCoach.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AIInterviewCoach.Application.Services
{
    public class SubmissionService : ISubmissionService
    {
        private readonly IAppDbContext _dbContext;
        private readonly ICodeExecutor _codeExecutor;
        private readonly ISubmissionFeedbackService _submissionFeedbackService;

        public SubmissionService(
            IAppDbContext dbContext,
            ICodeExecutor codeExecutor,
            ISubmissionFeedbackService submissionFeedbackService)
        {
            _dbContext = dbContext;
            _codeExecutor = codeExecutor;
            _submissionFeedbackService = submissionFeedbackService;
        }

        public async Task<SubmissionResponseDto> CreateSubmissionAsync(Guid candidateId, CreateSubmissionRequestDto request)
        {
            var problem = await _dbContext.Problems
                .Include(p => p.TestCases)
                .FirstOrDefaultAsync(p => p.Id == request.ProblemId);

            if (problem is null)
                throw new KeyNotFoundException("Problem not found.");

            InterviewSession? interviewSession = null;

            if (request.InterviewSessionId.HasValue)
            {
                interviewSession = await _dbContext.InterviewSessions
                    .Include(s => s.Interview)
                    .FirstOrDefaultAsync(s =>
                        s.Id == request.InterviewSessionId.Value &&
                        s.CandidateId == candidateId);

                if (interviewSession is null)
                    throw new KeyNotFoundException("Interview session not found.");

                if (interviewSession.Status != InterviewSessionStatus.InProgress)
                    throw new InvalidOperationException("Interview session is not active.");

                var belongsToInterview = await _dbContext.InterviewProblems
                    .AnyAsync(ip =>
                        ip.InterviewId == interviewSession.InterviewId &&
                        ip.ProblemId == request.ProblemId);

                if (!belongsToInterview)
                    throw new InvalidOperationException("Problem does not belong to this interview.");
            }

            var executionResult = await _codeExecutor.ExecuteAsync(
                problem,
                request.SourceCode,
                request.Language,
                problem.TestCases);
            var aiFeedbackResult = await _submissionFeedbackService.GenerateFeedbackAsync(
                BuildFeedbackContext(problem, request, executionResult),
                CancellationToken.None);

            var submission = new Submission
            {
                Id = Guid.NewGuid(),
                CandidateId = candidateId,
                ProblemId = request.ProblemId,
                InterviewSessionId = request.InterviewSessionId,
                Language = request.Language.Trim(),
                SourceCode = request.SourceCode,
                SubmittedAt = DateTime.UtcNow,
                TotalTests = executionResult.TotalTests,
                PassedTests = executionResult.PassedTests,
                ExecutionTimeMs = executionResult.TimeMs,
                MemoryKb = executionResult.MemoryKb,
                ExecutionOutput = executionResult.Output,
                AiFeedback = aiFeedbackResult.Content,
                Status = executionResult.Status
            };

            _dbContext.Submissions.Add(submission);
            await _dbContext.SaveChangesAsync();

            return MapToDto(submission, aiFeedbackResult.Source);
        }

        public async Task<IEnumerable<SubmissionResponseDto>> GetMySubmissionsAsync(Guid candidateId)
        {
            return await _dbContext.Submissions
                .AsNoTracking()
                .Where(s => s.CandidateId == candidateId)
                .OrderByDescending(s => s.SubmittedAt)
                .Select(s => new SubmissionResponseDto
                {
                    Id = s.Id,
                    CandidateId = s.CandidateId,
                    ProblemId = s.ProblemId,
                    InterviewSessionId = s.InterviewSessionId,
                    Language = s.Language,
                    SourceCode = s.SourceCode,
                    Status = s.Status.ToString(),
                    PassedTests = s.PassedTests,
                    TotalTests = s.TotalTests,
                    ExecutionTimeMs = s.ExecutionTimeMs,
                    MemoryKb = s.MemoryKb,
                    ExecutionOutput = s.ExecutionOutput,
                    AiFeedback = s.AiFeedback,
                    AiFeedbackSource = SubmissionFeedbackSourceResolver.ResolveSource(s.AiFeedback),
                    SubmittedAt = s.SubmittedAt
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<SubmissionResponseDto>> GetByInterviewSessionAsync(Guid interviewSessionId, Guid candidateId)
        {
            var sessionExists = await _dbContext.InterviewSessions
                .AnyAsync(s => s.Id == interviewSessionId && s.CandidateId == candidateId);

            if (!sessionExists)
                throw new KeyNotFoundException("Interview session not found.");

            return await _dbContext.Submissions
                .AsNoTracking()
                .Where(s => s.InterviewSessionId == interviewSessionId && s.CandidateId == candidateId)
                .OrderByDescending(s => s.SubmittedAt)
                .Select(s => new SubmissionResponseDto
                {
                    Id = s.Id,
                    CandidateId = s.CandidateId,
                    ProblemId = s.ProblemId,
                    InterviewSessionId = s.InterviewSessionId,
                    Language = s.Language,
                    SourceCode = s.SourceCode,
                    Status = s.Status.ToString(),
                    PassedTests = s.PassedTests,
                    TotalTests = s.TotalTests,
                    ExecutionTimeMs = s.ExecutionTimeMs,
                    MemoryKb = s.MemoryKb,
                    ExecutionOutput = s.ExecutionOutput,
                    AiFeedback = s.AiFeedback,
                    AiFeedbackSource = SubmissionFeedbackSourceResolver.ResolveSource(s.AiFeedback),
                    SubmittedAt = s.SubmittedAt
                })
                .ToListAsync();
        }

        public async Task ResetProblemAsync(Guid candidateId, Guid problemId, Guid? interviewSessionId)
        {
            if (interviewSessionId.HasValue)
            {
                var session = await _dbContext.InterviewSessions
                    .Include(s => s.Interview)
                    .FirstOrDefaultAsync(s =>
                        s.Id == interviewSessionId.Value &&
                        s.CandidateId == candidateId);

                if (session is null)
                    throw new KeyNotFoundException("Interview session not found.");

                if (session.Status != InterviewSessionStatus.InProgress)
                    throw new InvalidOperationException("Only in-progress interview sessions can be reset.");

                var belongsToInterview = await _dbContext.InterviewProblems
                    .AnyAsync(ip =>
                        ip.InterviewId == session.InterviewId &&
                        ip.ProblemId == problemId);

                if (!belongsToInterview)
                    throw new InvalidOperationException("Problem does not belong to this interview.");
            }
            else
            {
                var problemExists = await _dbContext.Problems.AnyAsync(p => p.Id == problemId);

                if (!problemExists)
                    throw new KeyNotFoundException("Problem not found.");
            }

            var submissions = await _dbContext.Submissions
                .Where(s =>
                    s.CandidateId == candidateId &&
                    s.ProblemId == problemId &&
                    s.InterviewSessionId == interviewSessionId)
                .ToListAsync();

            if (submissions.Count == 0)
                return;

            _dbContext.Submissions.RemoveRange(submissions);
            await _dbContext.SaveChangesAsync();
        }

        public async Task ResetInterviewSessionAsync(Guid candidateId, Guid interviewSessionId)
        {
            var session = await _dbContext.InterviewSessions
                .FirstOrDefaultAsync(s =>
                    s.Id == interviewSessionId &&
                    s.CandidateId == candidateId);

            if (session is null)
                throw new KeyNotFoundException("Interview session not found.");

            if (session.Status != InterviewSessionStatus.InProgress)
                throw new InvalidOperationException("Only in-progress interview sessions can be reset.");

            var submissions = await _dbContext.Submissions
                .Where(s =>
                    s.CandidateId == candidateId &&
                    s.InterviewSessionId == interviewSessionId)
                .ToListAsync();

            if (submissions.Count == 0)
                return;

            _dbContext.Submissions.RemoveRange(submissions);
            await _dbContext.SaveChangesAsync();
        }

        private static SubmissionFeedbackContextDto BuildFeedbackContext(
            Problem problem,
            CreateSubmissionRequestDto request,
            ExecutionResult executionResult)
        {
            return new SubmissionFeedbackContextDto
            {
                ProblemTitle = problem.Title,
                ProblemDescription = problem.Description,
                Difficulty = problem.Difficulty,
                Topic = problem.Topic,
                ConstraintsText = problem.ConstraintsText,
                ExampleInput = problem.ExampleInput,
                ExampleOutput = problem.ExampleOutput,
                Language = request.Language.Trim(),
                SourceCode = request.SourceCode,
                Status = executionResult.Status.ToString(),
                PassedTests = executionResult.PassedTests,
                TotalTests = executionResult.TotalTests,
                ExecutionOutput = executionResult.Output,
                IsPracticeMode = !request.InterviewSessionId.HasValue
            };
        }

        private static SubmissionResponseDto MapToDto(Submission submission, string? aiFeedbackSource = null)
        {
            return new SubmissionResponseDto
            {
                Id = submission.Id,
                CandidateId = submission.CandidateId,
                ProblemId = submission.ProblemId,
                InterviewSessionId = submission.InterviewSessionId,
                Language = submission.Language,
                SourceCode = submission.SourceCode,
                Status = submission.Status.ToString(),
                PassedTests = submission.PassedTests,
                TotalTests = submission.TotalTests,
                ExecutionTimeMs = submission.ExecutionTimeMs,
                MemoryKb = submission.MemoryKb,
                ExecutionOutput = submission.ExecutionOutput,
                AiFeedback = submission.AiFeedback,
                AiFeedbackSource = aiFeedbackSource ?? SubmissionFeedbackSourceResolver.ResolveSource(submission.AiFeedback),
                SubmittedAt = submission.SubmittedAt
            };
        }
    }
}
