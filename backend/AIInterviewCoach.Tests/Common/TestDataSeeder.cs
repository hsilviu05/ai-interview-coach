using AIInterviewCoach.Domain.Entities;
using AIInterviewCoach.Domain.Enums;
using AIInterviewCoach.Domain.Constants;
using AIInterviewCoach.Infrastructure.Persistence;

namespace AIInterviewCoach.Tests.Common
{
    public static class TestDataSeeder
    {
        public static User CreateCandidate(AppDbContext db, string? email = null)
        {
            return CreateUser(db, UserRole.Candidate, email);
        }

        public static User CreateInterviewer(AppDbContext db, string? email = null)
        {
            return CreateUser(db, UserRole.Interviewer, email);
        }

        public static User CreateAdmin(AppDbContext db, string? email = null)
        {
            return CreateUser(db, UserRole.Admin, email);
        }

        private static User CreateUser(AppDbContext db, UserRole role, string? email = null)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                FullName = $"{role} User",
                Email = email ?? $"{Guid.NewGuid():N}@test.com",
                PasswordHash = "hashed-password",
                UserRole = role,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.Users.Add(user);
            db.SaveChanges();

            return user;
        }

        public static Problem CreateProblem(
            AppDbContext db,
            Guid createdByUserId,
            int testCaseCount = 2,
            string? title = null,
            bool isPublic = true)
        {
            var problem = new Problem
            {
                Id = Guid.NewGuid(),
                Title = title ?? $"Problem-{Guid.NewGuid():N}",
                Description = "Test description",
                Difficulty = "Easy",
                Topic = "Arrays",
                ConstraintsText = "1 <= n <= 1000",
                ExampleInput = "[]",
                ExampleOutput = "[]",
                IsPublic = isPublic,
                CreatedByUserId = createdByUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.Problems.Add(problem);
            db.SaveChanges();

            for (var i = 0; i < testCaseCount; i++)
            {
                db.TestCases.Add(new TestCase
                {
                    Id = Guid.NewGuid(),
                    ProblemId = problem.Id,
                    Input = $"input-{i + 1}",
                    ExpectedOutput = $"output-{i + 1}",
                    IsHidden = false,
                    OrderIndex = i + 1
                });
            }

            db.SaveChanges();

            return problem;
        }

        public static Interview CreateInterview(
            AppDbContext db,
            Guid interviewerId,
            string? token = null,
            bool isActive = true)
        {
            var interview = new Interview
            {
                Id = Guid.NewGuid(),
                Title = "Backend Interview",
                PositionName = "Junior Backend Developer",
                Description = "Test interview",
                DurationMinutes = 60,
                AccessToken = token ?? Guid.NewGuid().ToString("N"),
                IsActive = isActive,
                InterviewerId = interviewerId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.Interviews.Add(interview);
            db.SaveChanges();

            return interview;
        }

        public static InterviewProblem AddProblemToInterview(
            AppDbContext db,
            Guid interviewId,
            Guid problemId,
            int points = 100,
            int orderIndex = 1)
        {
            var interviewProblem = new InterviewProblem
            {
                Id = Guid.NewGuid(),
                InterviewId = interviewId,
                ProblemId = problemId,
                Points = points,
                OrderIndex = orderIndex
            };

            db.InterviewProblems.Add(interviewProblem);
            db.SaveChanges();

            return interviewProblem;
        }

        public static InterviewSession CreateInterviewSession(
            AppDbContext db,
            Guid interviewId,
            Guid candidateId,
            InterviewSessionStatus status = InterviewSessionStatus.InProgress)
        {
            var session = new InterviewSession
            {
                Id = Guid.NewGuid(),
                InterviewId = interviewId,
                CandidateId = candidateId,
                StartedAt = DateTime.UtcNow,
                Status = status,
                TotalScore = 0
            };

            db.InterviewSessions.Add(session);
            db.SaveChanges();

            return session;
        }

        public static Submission CreateSubmission(
            AppDbContext db,
            Guid candidateId,
            Guid problemId,
            Guid? interviewSessionId,
            SubmissionStatus status,
            int passedTests = 0,
            int totalTests = 0,
            string? aiFeedback = null,
            string? aiFeedbackStatus = null)
        {
            var submission = new Submission
            {
                Id = Guid.NewGuid(),
                CandidateId = candidateId,
                ProblemId = problemId,
                InterviewSessionId = interviewSessionId,
                Language = "csharp",
                SourceCode = "public class Solution { }",
                Status = status,
                PassedTests = passedTests,
                TotalTests = totalTests,
                ExecutionTimeMs = 100,
                MemoryKb = 1024,
                AiFeedback = aiFeedback,
                AiFeedbackStatus = aiFeedbackStatus ?? SubmissionFeedbackStatuses.Ready,
                SubmittedAt = DateTime.UtcNow
            };

            db.Submissions.Add(submission);
            db.SaveChanges();

            return submission;
        }
    }
}
