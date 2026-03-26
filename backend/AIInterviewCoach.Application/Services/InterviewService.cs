using AIInterviewCoach.Application.DTOs.Interviews;
using AIInterviewCoach.Application.Interfaces.Services;
using AIInterviewCoach.Domain.Entities;
using AIInterviewCoach.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AIInterviewCoach.Application.Services
{
    public class InterviewService : IInterviewService
    {
        private readonly IAppDbContext _dbContext;

        public InterviewService(IAppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<InterviewResponseDto> CreateInterviewAsync(Guid interviewerId, CreateInterviewRequestDto request)
        {
            var interview = new Interview
            {
                Id = Guid.NewGuid(),
                Title = request.Title.Trim(),
                PositionName = request.PositionName.Trim(),
                Description = request.Description.Trim(),
                DurationMinutes = request.DurationMinutes,
                AccessToken = Guid.NewGuid().ToString("N"),
                IsActive = true,
                InterviewerId = interviewerId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.Interviews.Add(interview);
            await _dbContext.SaveChangesAsync();

            return MapInterviewToDto(interview, new List<InterviewProblemDto>());
        }

        public async Task<bool> AddProblemAsync(Guid interviewId, AddProblemToInterviewRequestDto request, Guid interviewerId)
        {
            var interview = await _dbContext.Interviews
                .Include(i => i.InterviewProblems)
                .FirstOrDefaultAsync(i => i.Id == interviewId);

            if (interview is null)
                return false;

            if (interview.InterviewerId != interviewerId)
                throw new UnauthorizedAccessException("You can only modify your own interviews.");

            var problemExists = await _dbContext.Problems.AnyAsync(p => p.Id == request.ProblemId);
            if (!problemExists)
                throw new KeyNotFoundException("Problem not found.");

            var alreadyAdded = interview.InterviewProblems.Any(ip => ip.ProblemId == request.ProblemId);
            if (alreadyAdded)
                throw new InvalidOperationException("Problem is already added to this interview.");

            var interviewProblem = new InterviewProblem
            {
                Id = Guid.NewGuid(),
                InterviewId = interviewId,
                ProblemId = request.ProblemId,
                OrderIndex = request.OrderIndex,
                Points = request.Points
            };

            _dbContext.InterviewProblems.Add(interviewProblem);
            await _dbContext.SaveChangesAsync();

            return true;
        }

        public async Task<InterviewResponseDto?> GetByIdAsync(Guid id)
        {
            var interview = await _dbContext.Interviews
                .AsNoTracking()
                .Include(i => i.InterviewProblems)
                    .ThenInclude(ip => ip.Problem)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (interview is null)
                return null;

            var problems = interview.InterviewProblems
                .OrderBy(ip => ip.OrderIndex)
                .Select(ip => new InterviewProblemDto
                {
                    ProblemId = ip.ProblemId,
                    Title = ip.Problem?.Title ?? string.Empty,
                    OrderIndex = ip.OrderIndex,
                    Points = ip.Points
                })
                .ToList();

            return MapInterviewToDto(interview, problems);
        }

        public async Task<InterviewResponseDto?> GetByTokenAsync(string token)
        {
            var interview = await _dbContext.Interviews
                .AsNoTracking()
                .Include(i => i.InterviewProblems)
                    .ThenInclude(ip => ip.Problem)
                .FirstOrDefaultAsync(i => i.AccessToken == token);

            if (interview is null)
                return null;

            var problems = interview.InterviewProblems
                .OrderBy(ip => ip.OrderIndex)
                .Select(ip => new InterviewProblemDto
                {
                    ProblemId = ip.ProblemId,
                    Title = ip.Problem?.Title ?? string.Empty,
                    OrderIndex = ip.OrderIndex,
                    Points = ip.Points
                })
                .ToList();

            return MapInterviewToDto(interview, problems);
        }

        public async Task<InterviewSessionResponseDto> StartSessionAsync(string token, Guid candidateId)
        {
            var interview = await _dbContext.Interviews
                .FirstOrDefaultAsync(i => i.AccessToken == token && i.IsActive);

            if (interview is null)
                throw new KeyNotFoundException("Interview not found or inactive.");

            var existingSession = await _dbContext.InterviewSessions
                .FirstOrDefaultAsync(s =>
                    s.InterviewId == interview.Id &&
                    s.CandidateId == candidateId &&
                    s.Status == InterviewSessionStatus.InProgress);

            if (existingSession is not null)
            {
                return MapSessionToDto(existingSession);
            }

            var session = new InterviewSession
            {
                Id = Guid.NewGuid(),
                InterviewId = interview.Id,
                CandidateId = candidateId,
                StartedAt = DateTime.UtcNow,
                Status = InterviewSessionStatus.InProgress,
                TotalScore = 0
            };

            _dbContext.InterviewSessions.Add(session);
            await _dbContext.SaveChangesAsync();

            return MapSessionToDto(session);
        }

        private static InterviewResponseDto MapInterviewToDto(Interview interview, List<InterviewProblemDto> problems)
        {
            return new InterviewResponseDto
            {
                Id = interview.Id,
                Title = interview.Title,
                PositionName = interview.PositionName,
                Description = interview.Description,
                DurationMinutes = interview.DurationMinutes,
                AccessToken = interview.AccessToken,
                IsActive = interview.IsActive,
                InterviewerId = interview.InterviewerId,
                CreatedAt = interview.CreatedAt,
                Problems = problems
            };
        }

        private static InterviewSessionResponseDto MapSessionToDto(InterviewSession session)
        {
            return new InterviewSessionResponseDto
            {
                Id = session.Id,
                InterviewId = session.InterviewId,
                CandidateId = session.CandidateId,
                StartedAt = session.StartedAt,
                SubmittedAt = session.SubmittedAt,
                Status = session.Status.ToString(),
                TotalScore = session.TotalScore
            };
        }
    }
}