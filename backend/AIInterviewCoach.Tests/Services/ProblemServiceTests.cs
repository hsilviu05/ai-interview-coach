using AIInterviewCoach.Application.DTOs.AdminAuditLogs;
using AIInterviewCoach.Application.DTOs.Problems;
using AIInterviewCoach.Application.Interfaces.Services;
using AIInterviewCoach.Application.Services;
using AIInterviewCoach.Application.Services.ProblemSignatures;
using AIInterviewCoach.Domain.Entities;
using AIInterviewCoach.Domain.Enums;
using AIInterviewCoach.Infrastructure.Services;
using AIInterviewCoach.Tests.Common;
using Microsoft.EntityFrameworkCore;

namespace AIInterviewCoach.Tests.Services
{
    public class ProblemServiceTests
    {
        [Fact]
        public async Task GetAllProblemsAsync_ShouldOnlyReturnPublicProblemsForCandidates()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var candidate = TestDataSeeder.CreateCandidate(db);

            var publicProblem = TestDataSeeder.CreateProblem(db, interviewer.Id, title: "Public", isPublic: true);
            TestDataSeeder.CreateProblem(db, interviewer.Id, title: "Private", isPublic: false);

            var service = CreateService(db);

            var results = (await service.GetAllProblemsAsync(candidate.Id, UserRole.Candidate)).ToList();

            Assert.Single(results);
            Assert.Equal(publicProblem.Id, results[0].Id);
        }

        [Fact]
        public async Task GetProblemByIdAsync_ShouldThrow_WhenCandidateRequestsPrivateProblem()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var candidate = TestDataSeeder.CreateCandidate(db);
            var privateProblem = TestDataSeeder.CreateProblem(db, interviewer.Id, isPublic: false);

            var service = CreateService(db);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.GetProblemByIdAsync(privateProblem.Id, candidate.Id, UserRole.Candidate));
        }

        [Fact]
        public async Task GetProblemByIdAsync_ShouldThrow_WhenInterviewerRequestsAnotherInterviewersPrivateProblem()
        {
            using var db = TestDbContextFactory.CreateContext();

            var owner = TestDataSeeder.CreateInterviewer(db);
            var other = TestDataSeeder.CreateInterviewer(db);
            var privateProblem = TestDataSeeder.CreateProblem(db, owner.Id, isPublic: false);

            var service = CreateService(db);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.GetProblemByIdAsync(privateProblem.Id, other.Id, UserRole.Interviewer));
        }

        [Fact]
        public async Task GetProblemByIdAsync_ShouldSucceed_WhenInterviewerRequestsTheirOwnPrivateProblem()
        {
            using var db = TestDbContextFactory.CreateContext();

            var owner = TestDataSeeder.CreateInterviewer(db);
            var privateProblem = TestDataSeeder.CreateProblem(db, owner.Id, isPublic: false);

            var service = CreateService(db);

            var result = await service.GetProblemByIdAsync(privateProblem.Id, owner.Id, UserRole.Interviewer);

            Assert.Equal(privateProblem.Id, result.Id);
        }

        [Fact]
        public async Task GetProblemByIdAsync_ShouldSucceed_WhenInterviewerRequestsAnotherInterviewersPublicProblem()
        {
            using var db = TestDbContextFactory.CreateContext();

            var owner = TestDataSeeder.CreateInterviewer(db);
            var other = TestDataSeeder.CreateInterviewer(db);
            var publicProblem = TestDataSeeder.CreateProblem(db, owner.Id, isPublic: true);

            var service = CreateService(db);

            var result = await service.GetProblemByIdAsync(publicProblem.Id, other.Id, UserRole.Interviewer);

            Assert.Equal(publicProblem.Id, result.Id);
        }

        [Fact]
        public async Task GetProblemByIdAsync_ShouldPopulateCentralizedStarterCode_WhenProblemUsesBlankStdinStarters()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var candidate = TestDataSeeder.CreateCandidate(db);
            var problem = TestDataSeeder.CreateProblem(db, interviewer.Id, isPublic: true);
            problem.ExecutionMode = ProblemExecutionModes.Stdin;
            problem.CsharpStarterCode = string.Empty;
            problem.PythonStarterCode = string.Empty;
            problem.CppStarterCode = string.Empty;
            db.SaveChanges();

            var service = CreateService(db);

            var result = await service.GetProblemByIdAsync(problem.Id, candidate.Id, UserRole.Candidate);

            Assert.Contains("using System;", result.CsharpStarterCode);
            Assert.Contains("import sys", result.PythonStarterCode);
            Assert.Contains("#include <algorithm>", result.CppStarterCode);
        }

        [Fact]
        public async Task GetTestCasesAsync_ShouldThrow_WhenInterviewerDoesNotOwnProblem()
        {
            using var db = TestDbContextFactory.CreateContext();

            var owner = TestDataSeeder.CreateInterviewer(db);
            var otherInterviewer = TestDataSeeder.CreateInterviewer(db);
            var problem = TestDataSeeder.CreateProblem(db, owner.Id, isPublic: false);

            var service = CreateService(db);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.GetTestCasesAsync(problem.Id, otherInterviewer.Id, isAdmin: false, includeHidden: true));
        }

        [Fact]
        public async Task AddTestCaseAsync_ShouldThrow_WhenInterviewerDoesNotOwnProblem()
        {
            using var db = TestDbContextFactory.CreateContext();

            var owner = TestDataSeeder.CreateInterviewer(db);
            var otherInterviewer = TestDataSeeder.CreateInterviewer(db);
            var problem = TestDataSeeder.CreateProblem(db, owner.Id, isPublic: false);

            var service = CreateService(db);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.AddTestCaseAsync(
                    problem.Id,
                    otherInterviewer.Id,
                    isAdmin: false,
                    new CreateTestCaseRequestDto
                    {
                        Input = "1 2",
                        ExpectedOutput = "3",
                        IsHidden = true,
                        OrderIndex = 99
                    }));
        }

        [Fact]
        public async Task DeleteProblemAsync_ShouldThrow_WhenProblemIsAttachedToInterview()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var problem = TestDataSeeder.CreateProblem(db, interviewer.Id);
            var interview = TestDataSeeder.CreateInterview(db, interviewer.Id);
            TestDataSeeder.AddProblemToInterview(db, interview.Id, problem.Id);

            var service = CreateService(db);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.DeleteProblemAsync(problem.Id, interviewer.Id, isAdmin: false));
        }

        [Fact]
        public async Task CreateProblemAsync_ShouldThrow_WhenFunctionSignatureHasDuplicateParameters()
        {
            using var db = TestDbContextFactory.CreateContext();

            var admin = TestDataSeeder.CreateAdmin(db);
            var service = CreateService(db);
            var request = BuildCreateRequest();
            request.Signature!.Parameters.Add(new ProblemSignatureParameterDto
            {
                Name = "NODECOUNT",
                Type = ProblemSignatureTypeKeys.Int
            });

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.CreateProblemAsync(admin.Id, request));

            Assert.Contains("declared more than once", exception.Message);
        }

        [Fact]
        public async Task UpdateProblemAsync_ShouldThrow_WhenRawTextSignatureDoesNotUseSingleStringParameter()
        {
            using var db = TestDbContextFactory.CreateContext();

            var admin = TestDataSeeder.CreateAdmin(db);
            var problem = TestDataSeeder.CreateProblem(db, admin.Id, title: "Editable Problem");
            var service = CreateService(db);
            var request = BuildUpdateRequest();
            request.Signature = new ProblemSignatureDefinitionDto
            {
                InputBindingMode = ProblemSignatureInputBindingModes.RawText,
                CsharpMethodName = "Solve",
                PythonMethodName = "solve",
                CppMethodName = "solve",
                ReturnType = ProblemSignatureTypeKeys.String,
                Parameters =
                [
                    new ProblemSignatureParameterDto
                    {
                        Name = "rawInput",
                        Type = ProblemSignatureTypeKeys.Int
                    }
                ]
            };

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.UpdateProblemAsync(problem.Id, admin.Id, request));

            Assert.Contains("Raw-text signatures must define exactly one string parameter.", exception.Message);
        }

        [Fact]
        public async Task CreateProblemAsync_ShouldWriteAdminAuditLog()
        {
            using var db = TestDbContextFactory.CreateContext();

            var admin = TestDataSeeder.CreateAdmin(db);
            var service = CreateService(db);

            var result = await service.CreateProblemAsync(admin.Id, BuildCreateRequest());

            var auditLog = await db.AdminAuditLogs.SingleAsync();
            Assert.Equal(admin.Id, auditLog.AdminUserId);
            Assert.Equal("problem.created", auditLog.ActionType);
            Assert.Equal(result.Id, auditLog.TargetId);
            Assert.Contains("Created problem", auditLog.Summary);
            Assert.Contains("function", auditLog.DetailsJson);
        }

        [Fact]
        public async Task UpdateProblemAsync_ShouldWriteAdminAuditLog()
        {
            using var db = TestDbContextFactory.CreateContext();

            var admin = TestDataSeeder.CreateAdmin(db);
            var problem = TestDataSeeder.CreateProblem(db, admin.Id, title: "Old Title");
            var service = CreateService(db);

            var updatedProblem = await service.UpdateProblemAsync(problem.Id, admin.Id, BuildUpdateRequest());

            Assert.NotNull(updatedProblem);

            var auditLog = await db.AdminAuditLogs.SingleAsync();
            Assert.Equal("problem.updated", auditLog.ActionType);
            Assert.Equal(problem.Id, auditLog.TargetId);
            Assert.Contains("Updated problem", auditLog.Summary);
            Assert.Contains("previousTitle", auditLog.DetailsJson);
            Assert.Contains("Old Title", auditLog.DetailsJson);
            Assert.Contains("Updated Title", auditLog.DetailsJson);
        }

        [Fact]
        public async Task AddTestCaseAsync_ShouldWriteAdminAuditLog_WhenAdminAddsTestCase()
        {
            using var db = TestDbContextFactory.CreateContext();

            var admin = TestDataSeeder.CreateAdmin(db);
            var problem = TestDataSeeder.CreateProblem(db, admin.Id, testCaseCount: 0, title: "Arrays Warmup");
            var service = CreateService(db);

            var created = await service.AddTestCaseAsync(
                problem.Id,
                admin.Id,
                isAdmin: true,
                new CreateTestCaseRequestDto
                {
                    Input = "1 2",
                    ExpectedOutput = "3",
                    IsHidden = true,
                    OrderIndex = 3
                });

            Assert.Equal(3, created.OrderIndex);

            var auditLog = await db.AdminAuditLogs.SingleAsync();
            Assert.Equal("testcase.created", auditLog.ActionType);
            Assert.Equal(created.Id, auditLog.TargetId);
            Assert.Contains("Added test case #3", auditLog.Summary);
            Assert.Contains("Arrays Warmup", auditLog.Summary);
        }

        [Fact]
        public async Task DeleteProblemAsync_ShouldWriteAdminAuditLog_WhenAdminDeletesProblem()
        {
            using var db = TestDbContextFactory.CreateContext();

            var admin = TestDataSeeder.CreateAdmin(db);
            var problem = TestDataSeeder.CreateProblem(db, admin.Id, title: "Delete Me");
            var service = CreateService(db);

            var deleted = await service.DeleteProblemAsync(problem.Id, admin.Id, isAdmin: true);

            Assert.True(deleted);

            var auditLog = await db.AdminAuditLogs.SingleAsync();
            Assert.Equal("problem.deleted", auditLog.ActionType);
            Assert.Equal(problem.Id, auditLog.TargetId);
            Assert.Contains("Delete Me", auditLog.Summary);
        }

        [Fact]
        public async Task ReplaceCatalogWithStarterSetAsync_ShouldResetWorkspaceAndCreateStarterProblems()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var candidate = TestDataSeeder.CreateCandidate(db);
            var problem = TestDataSeeder.CreateProblem(db, interviewer.Id);
            var interview = TestDataSeeder.CreateInterview(db, interviewer.Id);
            var session = TestDataSeeder.CreateInterviewSession(db, interview.Id, candidate.Id);
            TestDataSeeder.AddProblemToInterview(db, interview.Id, problem.Id);
            TestDataSeeder.CreateSubmission(db, candidate.Id, problem.Id, session.Id, SubmissionStatus.Accepted);

            db.CandidateStatistics.Add(new CandidateStatistic
            {
                Id = Guid.NewGuid(),
                CandidateId = candidate.Id,
                ProblemsSolved = 3,
                TotalSubmissions = 8,
                AccuracyRate = 67.5m,
                AverageExecutionTimeMs = 101,
                UpdatedAt = DateTime.UtcNow
            });
            db.SaveChanges();

            var service = CreateService(db);

            var result = await service.ReplaceCatalogWithStarterSetAsync(interviewer.Id);

            Assert.Equal(1, result.DeletedProblemCount);
            Assert.Equal(1, result.DeletedInterviewCount);
            Assert.Equal(1, result.DeletedSubmissionCount);
            Assert.Equal(4, result.CreatedProblemCount);
            Assert.Contains("Two Sum", result.CreatedProblemTitles);

            var problems = await db.Problems
                .Include(existingProblem => existingProblem.TestCases)
                .ToListAsync();

            Assert.Equal(4, problems.Count);
            Assert.All(problems, existingProblem =>
            {
                Assert.Equal(ProblemExecutionModes.FunctionSignature, existingProblem.ExecutionMode);
                Assert.Equal(interviewer.Id, existingProblem.CreatedByUserId);
                Assert.NotEmpty(existingProblem.TestCases);
                Assert.False(string.IsNullOrWhiteSpace(existingProblem.SignatureDefinitionJson));
                Assert.False(string.IsNullOrWhiteSpace(existingProblem.CsharpHarnessTemplate));
                Assert.Contains("{{candidate_code}}", existingProblem.CsharpHarnessTemplate);
            });

            Assert.Empty(await db.Interviews.ToListAsync());
            Assert.Empty(await db.InterviewProblems.ToListAsync());
            Assert.Empty(await db.InterviewSessions.ToListAsync());
            Assert.Empty(await db.Submissions.ToListAsync());

            var candidateStatistic = await db.CandidateStatistics.SingleAsync();
            Assert.Equal(0, candidateStatistic.ProblemsSolved);
            Assert.Equal(0, candidateStatistic.TotalSubmissions);
            Assert.Equal(0, candidateStatistic.AccuracyRate);
            Assert.Equal(0, candidateStatistic.AverageExecutionTimeMs);

            var auditLog = await db.AdminAuditLogs.SingleAsync();
            Assert.Equal("catalog.replaced", auditLog.ActionType);
            Assert.Contains("starter set", auditLog.Summary);
            Assert.Contains("Two Sum", auditLog.DetailsJson);
        }

        [Fact]
        public async Task ReplaceCatalogWithStarterSetAsync_ShouldRollback_WhenFinalStepFails()
        {
            using var sqliteScope = TestDbContextFactory.CreateSqliteContext();
            var db = sqliteScope.Context;

            var admin = TestDataSeeder.CreateAdmin(db);
            var candidate = TestDataSeeder.CreateCandidate(db);
            var originalProblem = TestDataSeeder.CreateProblem(db, admin.Id, title: "Original Problem");
            var originalInterview = TestDataSeeder.CreateInterview(db, admin.Id);
            var originalSession = TestDataSeeder.CreateInterviewSession(db, originalInterview.Id, candidate.Id);
            TestDataSeeder.AddProblemToInterview(db, originalInterview.Id, originalProblem.Id);
            TestDataSeeder.CreateSubmission(db, candidate.Id, originalProblem.Id, originalSession.Id, SubmissionStatus.Accepted);

            db.CandidateStatistics.Add(new CandidateStatistic
            {
                Id = Guid.NewGuid(),
                CandidateId = candidate.Id,
                ProblemsSolved = 5,
                TotalSubmissions = 9,
                AccuracyRate = 88.8m,
                AverageExecutionTimeMs = 123,
                UpdatedAt = DateTime.UtcNow
            });
            db.SaveChanges();

            var service = new ProblemService(db, new ThrowingAdminAuditService("catalog.replaced"));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ReplaceCatalogWithStarterSetAsync(admin.Id));

            using var verificationDb = sqliteScope.CreateAdditionalContext();

            Assert.Equal(1, await verificationDb.Problems.CountAsync());
            Assert.Equal("Original Problem", (await verificationDb.Problems.SingleAsync()).Title);
            Assert.Equal(1, await verificationDb.Interviews.CountAsync());
            Assert.Equal(1, await verificationDb.InterviewSessions.CountAsync());
            Assert.Equal(1, await verificationDb.Submissions.CountAsync());
            Assert.Equal(2, await verificationDb.TestCases.CountAsync());
            Assert.Empty(await verificationDb.AdminAuditLogs.ToListAsync());

            var statistics = await verificationDb.CandidateStatistics.SingleAsync();
            Assert.Equal(5, statistics.ProblemsSolved);
            Assert.Equal(9, statistics.TotalSubmissions);
            Assert.Equal(88.8m, statistics.AccuracyRate);
            Assert.Equal(123, statistics.AverageExecutionTimeMs);
        }

        // ── GetAllProblemsAsync ───────────────────────────────────────────────

        [Fact]
        public async Task GetAllProblemsAsync_ShouldReturnAll_ForAdmin()
        {
            using var db = TestDbContextFactory.CreateContext();

            var admin = TestDataSeeder.CreateAdmin(db);
            var interviewer = TestDataSeeder.CreateInterviewer(db);
            TestDataSeeder.CreateProblem(db, interviewer.Id, title: "Public", isPublic: true);
            TestDataSeeder.CreateProblem(db, interviewer.Id, title: "Private", isPublic: false);

            var service = CreateService(db);
            var results = (await service.GetAllProblemsAsync(admin.Id, UserRole.Admin)).ToList();

            Assert.Equal(2, results.Count);
        }

        [Fact]
        public async Task GetAllProblemsAsync_ShouldReturnPublicAndOwnPrivate_ForInterviewer()
        {
            using var db = TestDbContextFactory.CreateContext();

            var owner = TestDataSeeder.CreateInterviewer(db);
            var other = TestDataSeeder.CreateInterviewer(db);
            TestDataSeeder.CreateProblem(db, owner.Id, title: "Owner Public", isPublic: true);
            TestDataSeeder.CreateProblem(db, owner.Id, title: "Owner Private", isPublic: false);
            TestDataSeeder.CreateProblem(db, other.Id, title: "Other Public", isPublic: true);
            TestDataSeeder.CreateProblem(db, other.Id, title: "Other Private", isPublic: false);

            var service = CreateService(db);
            var results = (await service.GetAllProblemsAsync(owner.Id, UserRole.Interviewer)).ToList();

            Assert.Equal(3, results.Count);
            Assert.Contains(results, p => p.Title == "Owner Public");
            Assert.Contains(results, p => p.Title == "Owner Private");
            Assert.Contains(results, p => p.Title == "Other Public");
            Assert.DoesNotContain(results, p => p.Title == "Other Private");
        }

        // ── UpdateProblemAsync ────────────────────────────────────────────────

        [Fact]
        public async Task UpdateProblemAsync_ShouldReturnNull_WhenProblemNotFound()
        {
            using var db = TestDbContextFactory.CreateContext();

            var admin = TestDataSeeder.CreateAdmin(db);
            var service = CreateService(db);

            var result = await service.UpdateProblemAsync(Guid.NewGuid(), admin.Id, BuildUpdateRequest());

            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateProblemAsync_ShouldThrow_WhenNonOwnerTriesToUpdate()
        {
            using var db = TestDbContextFactory.CreateContext();

            var owner = TestDataSeeder.CreateInterviewer(db);
            var other = TestDataSeeder.CreateInterviewer(db);
            var problem = TestDataSeeder.CreateProblem(db, owner.Id);

            var service = CreateService(db);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.UpdateProblemAsync(problem.Id, other.Id, BuildUpdateRequest()));
        }

        // ── DeleteProblemAsync ────────────────────────────────────────────────

        [Fact]
        public async Task DeleteProblemAsync_ShouldReturnFalse_WhenProblemNotFound()
        {
            using var db = TestDbContextFactory.CreateContext();

            var admin = TestDataSeeder.CreateAdmin(db);
            var service = CreateService(db);

            var result = await service.DeleteProblemAsync(Guid.NewGuid(), admin.Id, isAdmin: true);

            Assert.False(result);
        }

        [Fact]
        public async Task DeleteProblemAsync_ShouldThrow_WhenNonOwnerTriesToDelete()
        {
            using var db = TestDbContextFactory.CreateContext();

            var owner = TestDataSeeder.CreateInterviewer(db);
            var other = TestDataSeeder.CreateInterviewer(db);
            var problem = TestDataSeeder.CreateProblem(db, owner.Id);

            var service = CreateService(db);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.DeleteProblemAsync(problem.Id, other.Id, isAdmin: false));
        }

        [Fact]
        public async Task DeleteProblemAsync_ShouldSucceed_AndNotWriteAuditLog_WhenOwnerDeletesAsNonAdmin()
        {
            using var db = TestDbContextFactory.CreateContext();

            var owner = TestDataSeeder.CreateInterviewer(db);
            var problem = TestDataSeeder.CreateProblem(db, owner.Id);

            var service = CreateService(db);

            var result = await service.DeleteProblemAsync(problem.Id, owner.Id, isAdmin: false);

            Assert.True(result);
            Assert.Equal(0, db.Problems.Count());
            Assert.Equal(0, db.AdminAuditLogs.Count());
        }

        // ── GetTestCasesAsync ─────────────────────────────────────────────────

        [Fact]
        public async Task GetTestCasesAsync_ShouldThrow_WhenProblemNotFound()
        {
            using var db = TestDbContextFactory.CreateContext();

            var admin = TestDataSeeder.CreateAdmin(db);
            var service = CreateService(db);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.GetTestCasesAsync(Guid.NewGuid(), admin.Id, isAdmin: true, includeHidden: true));
        }

        [Fact]
        public async Task GetTestCasesAsync_ShouldFilterHiddenTestCases_WhenIncludeHiddenIsFalse()
        {
            using var db = TestDbContextFactory.CreateContext();

            var owner = TestDataSeeder.CreateInterviewer(db);
            var problem = TestDataSeeder.CreateProblem(db, owner.Id, testCaseCount: 0);
            db.TestCases.Add(new Domain.Entities.TestCase
            {
                Id = Guid.NewGuid(), ProblemId = problem.Id,
                Input = "1", ExpectedOutput = "1", IsHidden = false, OrderIndex = 1
            });
            db.TestCases.Add(new Domain.Entities.TestCase
            {
                Id = Guid.NewGuid(), ProblemId = problem.Id,
                Input = "2", ExpectedOutput = "2", IsHidden = true, OrderIndex = 2
            });
            db.SaveChanges();

            var service = CreateService(db);

            var visible = (await service.GetTestCasesAsync(problem.Id, owner.Id, isAdmin: false, includeHidden: false)).ToList();
            var all = (await service.GetTestCasesAsync(problem.Id, owner.Id, isAdmin: false, includeHidden: true)).ToList();

            Assert.Single(visible);
            Assert.Equal(2, all.Count);
        }

        // ── AddTestCaseAsync ──────────────────────────────────────────────────

        [Fact]
        public async Task AddTestCaseAsync_ShouldThrow_WhenProblemNotFound()
        {
            using var db = TestDbContextFactory.CreateContext();

            var owner = TestDataSeeder.CreateInterviewer(db);
            var service = CreateService(db);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.AddTestCaseAsync(Guid.NewGuid(), owner.Id, isAdmin: false,
                    new CreateTestCaseRequestDto { Input = "1", ExpectedOutput = "1", IsHidden = false, OrderIndex = 1 }));
        }

        [Fact]
        public async Task AddTestCaseAsync_ShouldSucceed_AndNotWriteAuditLog_WhenNonAdminOwnerAdds()
        {
            using var db = TestDbContextFactory.CreateContext();

            var owner = TestDataSeeder.CreateInterviewer(db);
            var problem = TestDataSeeder.CreateProblem(db, owner.Id, testCaseCount: 0);

            var service = CreateService(db);

            var result = await service.AddTestCaseAsync(
                problem.Id, owner.Id, isAdmin: false,
                new CreateTestCaseRequestDto { Input = "in", ExpectedOutput = "out", IsHidden = false, OrderIndex = 1 });

            Assert.Equal(problem.Id, result.ProblemId);
            Assert.Equal("in", result.Input);
            Assert.Equal(0, db.AdminAuditLogs.Count());
        }

        // ── CreateProblemAsync ────────────────────────────────────────────────

        [Fact]
        public async Task CreateProblemAsync_ShouldCreateStdinProblem_WithStarterCode()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var service = CreateService(db);

            var result = await service.CreateProblemAsync(interviewer.Id, new CreateProblemRequestDto
            {
                Title = "Sum Two Numbers",
                Description = "Add two integers.",
                Difficulty = "Easy",
                Topic = "Math",
                ConstraintsText = "1 <= n <= 100",
                ExampleInput = "3 5",
                ExampleOutput = "8",
                ExecutionMode = ProblemExecutionModes.Stdin,
                CsharpStarterCode = "// csharp",
                PythonStarterCode = "# python",
                CppStarterCode = "// cpp",
                IsPublic = true
            });

            Assert.Equal(ProblemExecutionModes.Stdin, result.ExecutionMode);
            Assert.Null(result.Signature);
            Assert.Equal("// csharp", result.CsharpStarterCode);

            var stored = db.Problems.Single();
            Assert.Equal(ProblemExecutionModes.Stdin, stored.ExecutionMode);
            Assert.Null(stored.SignatureDefinitionJson);
        }

        [Fact]
        public async Task CreateProblemAsync_ShouldThrow_WhenStdinProblemMissingStarterCode()
        {
            using var db = TestDbContextFactory.CreateContext();

            var interviewer = TestDataSeeder.CreateInterviewer(db);
            var service = CreateService(db);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.CreateProblemAsync(interviewer.Id, new CreateProblemRequestDto
                {
                    Title = "No Starters",
                    Description = "desc",
                    Difficulty = "Easy",
                    Topic = "Math",
                    ConstraintsText = "",
                    ExampleInput = "",
                    ExampleOutput = "",
                    ExecutionMode = ProblemExecutionModes.Stdin,
                    CsharpStarterCode = "",
                    PythonStarterCode = "",
                    CppStarterCode = "",
                    IsPublic = false
                }));
        }

        private static ProblemService CreateService(Infrastructure.Persistence.AppDbContext db) =>
            new(db, new AdminAuditService(db, new ApplicationObservabilityService()));

        private static CreateProblemRequestDto BuildCreateRequest() =>
            new()
            {
                Title = "Graph Traversal",
                Description = "Traverse a graph safely.",
                Difficulty = "Medium",
                Topic = "Graphs",
                ConstraintsText = "1 <= n <= 1000",
                ExampleInput = "{\"nodes\":[]}",
                ExampleOutput = "[]",
                ExecutionMode = ProblemExecutionModes.FunctionSignature,
                Signature = new ProblemSignatureDefinitionDto
                {
                    InputBindingMode = ProblemSignatureInputBindingModes.JsonObject,
                    CsharpMethodName = "Solve",
                    PythonMethodName = "solve",
                    CppMethodName = "solve",
                    ReturnType = ProblemSignatureTypeKeys.Int,
                    Parameters =
                    [
                        new ProblemSignatureParameterDto
                        {
                            Name = "nodeCount",
                            Type = ProblemSignatureTypeKeys.Int
                        }
                    ]
                },
                IsPublic = true
            };

        private static UpdateProblemRequestDto BuildUpdateRequest() =>
            new()
            {
                Title = "Updated Title",
                Description = "Updated description",
                Difficulty = "Hard",
                Topic = "Dynamic Programming",
                ConstraintsText = "1 <= n <= 5000",
                ExampleInput = "{\"items\":[1,2,3]}",
                ExampleOutput = "42",
                ExecutionMode = ProblemExecutionModes.FunctionSignature,
                Signature = new ProblemSignatureDefinitionDto
                {
                    InputBindingMode = ProblemSignatureInputBindingModes.JsonObject,
                    CsharpMethodName = "Solve",
                    PythonMethodName = "solve",
                    CppMethodName = "solve",
                    ReturnType = ProblemSignatureTypeKeys.Int,
                    Parameters =
                    [
                        new ProblemSignatureParameterDto
                        {
                            Name = "capacity",
                            Type = ProblemSignatureTypeKeys.Int
                        }
                    ]
                },
                IsPublic = false
            };

        private sealed class ThrowingAdminAuditService : IAdminAuditService
        {
            private readonly string _actionTypeToThrow;

            public ThrowingAdminAuditService(string actionTypeToThrow)
            {
                _actionTypeToThrow = actionTypeToThrow;
            }

            public Task RecordAsync(AdminAuditLogWriteRequestDto request, CancellationToken cancellationToken = default)
            {
                if (request.ActionType == _actionTypeToThrow)
                {
                    throw new InvalidOperationException("Simulated audit write failure.");
                }

                return Task.CompletedTask;
            }

            public Task<IReadOnlyList<AdminAuditLogResponseDto>> GetRecentLogsAsync(int take = 25, CancellationToken cancellationToken = default)
            {
                return Task.FromResult<IReadOnlyList<AdminAuditLogResponseDto>>(Array.Empty<AdminAuditLogResponseDto>());
            }
        }
    }
}
