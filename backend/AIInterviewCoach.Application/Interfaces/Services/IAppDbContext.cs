using AIInterviewCoach.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace AIInterviewCoach.Application.Interfaces.Services
{
    public interface IAppDbContext
    {
        DbSet<User> Users { get; }
        DbSet<CandidateStatistic> CandidateStatistics { get; }
        DbSet<AdminAuditLog> AdminAuditLogs { get; }
        DbSet<Problem> Problems { get; }
        DbSet<TestCase> TestCases { get; }
        DbSet<Interview> Interviews { get; }
        DbSet<InterviewProblem> InterviewProblems { get; }
        DbSet<InterviewSession> InterviewSessions { get; }
        DbSet<Submission> Submissions { get; }
        DatabaseFacade Database { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
