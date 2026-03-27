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

        public SubmissionService(IAppDbContext dbContext)
        {
            _dbContext = dbContext;
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

            var totalTests = problem.TestCases.Count;

            var submission = new Submission
            {
                Id = Guid.NewGuid(),
                CandidateId = candidateId,
                ProblemId = request.ProblemId,
                InterviewSessionId = request.InterviewSessionId,
                Language = request.Language.Trim(),
                SourceCode = request.SourceCode,
                SubmittedAt = DateTime.UtcNow,
                TotalTests = totalTests,
                PassedTests = totalTests > 0 ? totalTests / 2 : 0,
                ExecutionTimeMs = 120,
                MemoryKb = 2048
            };

            submission.Status = submission.TotalTests > 0 && submission.PassedTests == submission.TotalTests
                ? SubmissionStatus.Accepted
                : SubmissionStatus.WrongAnswer;

            _dbContext.Submissions.Add(submission);
            await _dbContext.SaveChangesAsync();

            return MapToDto(submission);
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
                    SubmittedAt = s.SubmittedAt
                })
                .ToListAsync();
        }

        private static SubmissionResponseDto MapToDto(Submission submission)
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
                SubmittedAt = submission.SubmittedAt
            };
        }
    }
}