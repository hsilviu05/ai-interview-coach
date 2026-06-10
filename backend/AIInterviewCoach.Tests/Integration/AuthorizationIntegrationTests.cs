using System.Net;
using AIInterviewCoach.Tests.Common;

namespace AIInterviewCoach.Tests.Integration
{
    // Rule: InterviewerWorkspaceAccess = Interviewer + Admin
    //       CandidateWorkspaceAccess   = Candidate  + Admin
    //       Admin*                     = Admin only
    //
    // Ambiguous rule surfaced:
    //   GET /api/interviews/token/{token} carries [EnableRateLimiting("PublicInterviewTokenAccess")]
    //   (name implies unauthenticated access) but inherits the class-level [Authorize] and therefore
    //   requires authentication. The current behavior is 401 for unauthenticated callers; the rate
    //   limiter name suggests the original intent may have been to allow public lookup before login.
    //   Proposed rule: keep auth required — the frontend handles redirect-to-login before token lookup.
    public class AuthorizationIntegrationTests
    {
        // ── Unauthenticated → 401 ──────────────────────────────────────────────────

        [Fact]
        public async Task UnauthenticatedRequest_ReturnsUnauthorized_ForInterviewerRoute()
        {
            await using var factory = new TestApiApplicationFactory();
            using var client = factory.CreateClient();

            var response = await client.GetAsync("/api/interviews");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task UnauthenticatedRequest_ReturnsUnauthorized_ForCandidateRoute()
        {
            await using var factory = new TestApiApplicationFactory();
            using var client = factory.CreateClient();

            var response = await client.GetAsync("/api/submissions/me");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task UnauthenticatedRequest_ReturnsUnauthorized_ForAdminRoute()
        {
            await using var factory = new TestApiApplicationFactory();
            using var client = factory.CreateClient();

            var response = await client.GetAsync("/api/admin/audit-logs");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task UnauthenticatedRequest_ReturnsUnauthorized_ForTokenLookup()
        {
            await using var factory = new TestApiApplicationFactory();
            using var client = factory.CreateClient();

            // Class-level [Authorize] on InterviewsController requires authentication even
            // though the rate limiter name ("PublicInterviewTokenAccess") might imply otherwise.
            var response = await client.GetAsync("/api/interviews/token/any-token");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        // ── Wrong role → 403 ──────────────────────────────────────────────────────

        [Fact]
        public async Task CandidateRole_ReturnsForbidden_WhenListingInterviews()
        {
            await using var factory = new TestApiApplicationFactory();
            using var client = await factory.CreateAuthenticatedClientAsync("candidate@test.com");

            var response = await client.GetAsync("/api/interviews");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CandidateRole_ReturnsForbidden_WhenCreatingInterview()
        {
            await using var factory = new TestApiApplicationFactory();
            using var client = await factory.CreateAuthenticatedClientAsync("candidate@test.com");

            var response = await client.PostAsync("/api/interviews",
                new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task InterviewerRole_ReturnsForbidden_WhenListingOwnSubmissions()
        {
            await using var factory = new TestApiApplicationFactory();
            using var client = await factory.CreateAuthenticatedClientAsync("recruiter@test.com");

            var response = await client.GetAsync("/api/submissions/me");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task InterviewerRole_ReturnsForbidden_WhenStartingInterviewSession()
        {
            await using var factory = new TestApiApplicationFactory();
            using var client = await factory.CreateAuthenticatedClientAsync("recruiter@test.com");

            // POST /token/{token}/start requires CandidateWorkspaceAccess; interviewers are excluded.
            var response = await client.PostAsync("/api/interviews/token/any-token/start", null);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CandidateRole_ReturnsForbidden_WhenAccessingAdminAuditLogs()
        {
            await using var factory = new TestApiApplicationFactory();
            using var client = await factory.CreateAuthenticatedClientAsync("candidate@test.com");

            var response = await client.GetAsync("/api/admin/audit-logs");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task InterviewerRole_ReturnsForbidden_WhenAccessingAdminAuditLogs()
        {
            await using var factory = new TestApiApplicationFactory();
            using var client = await factory.CreateAuthenticatedClientAsync("recruiter@test.com");

            var response = await client.GetAsync("/api/admin/audit-logs");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        // ── Admin cross-role access ───────────────────────────────────────────────

        [Fact]
        public async Task AdminRole_CanAccessInterviewerRoutes()
        {
            await using var factory = new TestApiApplicationFactory();
            using var client = await factory.CreateAuthenticatedClientAsync("admin@test.com");

            var response = await client.GetAsync("/api/interviews");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task AdminRole_CanAccessCandidateRoutes()
        {
            await using var factory = new TestApiApplicationFactory();
            using var client = await factory.CreateAuthenticatedClientAsync("admin@test.com");

            var response = await client.GetAsync("/api/submissions/me");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task AdminRole_CanAccessAdminRoutes()
        {
            await using var factory = new TestApiApplicationFactory();
            using var client = await factory.CreateAuthenticatedClientAsync("admin@test.com");

            var response = await client.GetAsync("/api/admin/audit-logs");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
