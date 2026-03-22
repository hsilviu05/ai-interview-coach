using AIInterviewCoach.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using AIInterviewCoach.Application.Interfaces.Services;
using System.Security.Cryptography.X509Certificates;

namespace AIInterviewCoach.Infrastructure.Persistence
{
    public class AppDbContext : DbContext, IAppDbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        public DbSet<User> Users => Set<User>();
        public DbSet<CandidateStatistic> CandidateStatistics => Set<CandidateStatistic>();

        public DbSet<Problem> Problems => Set<Problem>();
        public DbSet<TestCase> TestCases => Set<TestCase>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.FullName).HasMaxLength(200).IsRequired();
                entity.Property(x => x.Email).HasMaxLength(200).IsRequired();
                entity.Property(x => x.PasswordHash).IsRequired();
                entity.HasIndex(x => x.Email).IsUnique();
            });

            modelBuilder.Entity<CandidateStatistic>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasOne(x => x.Candidate)
                    .WithOne(x => x.candidateStatistics)
                    .HasForeignKey<CandidateStatistic>(x => x.CandidateId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Problem>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
                entity.Property(x => x.Difficulty).HasMaxLength(50).IsRequired();
                entity.Property(x => x.Topic).HasMaxLength(100).IsRequired();

                entity.HasOne(x => x.CreatedBy)
                    .WithMany(X => X.Problems)
                    .HasForeignKey(x => x.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<TestCase>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasOne(x => x.Problem)
                    .WithMany(x => x.TestCases)
                    .HasForeignKey(x => x.ProblemId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}