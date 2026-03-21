using AIInterviewCoach.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AIInterviewCoach.Application.Interfaces.Services
{
    public interface IAppDbContext
    {
        DbSet<User> Users { get; }
        DbSet<CandidateStatistic> CandidateStatistics { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}