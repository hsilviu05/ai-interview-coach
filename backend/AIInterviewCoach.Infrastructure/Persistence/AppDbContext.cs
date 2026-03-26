using AIInterviewCoach.Application.Interfaces.Services;
using AIInterviewCoach.Domain.Entities;
using Microsoft.EntityFrameworkCore;

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
        public DbSet<Interview> Interviews => Set<Interview>();
        public DbSet<InterviewProblem> InterviewProblems => Set<InterviewProblem>();
        public DbSet<InterviewSession> InterviewSessions => Set<InterviewSession>();
        public DbSet<Submission> Submissions => Set<Submission>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.FullName)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.Email)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.PasswordHash)
                    .IsRequired();

                entity.HasIndex(x => x.Email)
                    .IsUnique();
            });

            modelBuilder.Entity<CandidateStatistic>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasOne(x => x.Candidate)
                    .WithOne(x => x.CandidateStatistics)
                    .HasForeignKey<CandidateStatistic>(x => x.CandidateId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Problem>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Title)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.Difficulty)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.Topic)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.HasOne(x => x.CreatedBy)
                    .WithMany(x => x.Problems)
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

            modelBuilder.Entity<Interview>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Title)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.PositionName)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.Description)
                    .HasMaxLength(2000);

                entity.Property(x => x.AccessToken)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.HasMany(x => x.InterviewProblems)
                    .WithOne(x => x.Interview)
                    .HasForeignKey(x => x.InterviewId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(x => x.Sessions)
                    .WithOne(x => x.Interview)
                    .HasForeignKey(x => x.InterviewId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<InterviewProblem>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasOne(x => x.Interview)
                    .WithMany(x => x.InterviewProblems)
                    .HasForeignKey(x => x.InterviewId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.Problem)
                    .WithMany()
                    .HasForeignKey(x => x.ProblemId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<InterviewSession>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasOne(x => x.Interview)
                    .WithMany(x => x.Sessions)
                    .HasForeignKey(x => x.InterviewId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.Candidate)
                    .WithMany()
                    .HasForeignKey(x => x.CandidateId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(x => x.Submissions)
                    .WithOne(x => x.InterviewSession)
                    .HasForeignKey(x => x.InterviewSessionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Submission>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Language)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.SourceCode)
                    .IsRequired();

                entity.HasOne(x => x.Candidate)
                    .WithMany()
                    .HasForeignKey(x => x.CandidateId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Problem)
                    .WithMany()
                    .HasForeignKey(x => x.ProblemId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.InterviewSession)
                    .WithMany(x => x.Submissions)
                    .HasForeignKey(x => x.InterviewSessionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}