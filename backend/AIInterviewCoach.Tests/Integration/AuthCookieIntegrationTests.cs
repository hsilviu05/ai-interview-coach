using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AIInterviewCoach.Application.DTOs.Auth;
using AIInterviewCoach.Tests.Common;

namespace AIInterviewCoach.Tests.Integration
{
    public sealed class AuthCookieIntegrationTests
    {
        [Fact]
        public async Task Login_ShouldSetHttpOnlyCookie_AndAllowMeEndpoint()
        {
            await using var factory = new TestApiApplicationFactory(useTestAuthentication: false);
            using var client = factory.CreateClient();

            var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
            {
                Email = "candidate@test.com",
                Password = "Password123!"
            });

            Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
            Assert.True(
                loginResponse.Headers.TryGetValues("Set-Cookie", out var cookieHeaders) &&
                cookieHeaders.Any(header =>
                    header.Contains("aic_auth=", StringComparison.Ordinal) &&
                    header.Contains("HttpOnly", StringComparison.OrdinalIgnoreCase)),
                "Expected login to set an HttpOnly auth cookie.");

            using var loginPayload = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
            Assert.False(
                loginPayload.RootElement.TryGetProperty("token", out _),
                "Login response should not expose the JWT in the response body.");

            var meResponse = await client.GetAsync("/api/auth/me");
            Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);

            var me = await meResponse.Content.ReadFromJsonAsync<AuthSessionResponseDto>();
            Assert.NotNull(me);
            Assert.Equal("candidate@test.com", me!.Email);
            Assert.Equal("Candidate", me.Role);
        }

        [Fact]
        public async Task Logout_ShouldClearCookieAndEndSession()
        {
            await using var factory = new TestApiApplicationFactory(useTestAuthentication: false);
            using var client = factory.CreateClient();

            await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
            {
                Email = "candidate@test.com",
                Password = "Password123!"
            });

            var logoutResponse = await client.PostAsync("/api/auth/logout", null);
            Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

            var meResponse = await client.GetAsync("/api/auth/me");
            Assert.Equal(HttpStatusCode.Unauthorized, meResponse.StatusCode);
        }
    }
}
