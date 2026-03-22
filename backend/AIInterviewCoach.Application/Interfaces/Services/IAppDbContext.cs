using AIInterviewCoach.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AIInterviewCoach.Application.Interfaces.Services
{
    public interface IAppDbContext
    {
        DbSet<User> Users { get; }
        DbSet<CandidateStatistic> CandidateStatistics { get; }
        DbSet<Problem> Problems { get; }
        DbSet<TestCase> TestCases { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}