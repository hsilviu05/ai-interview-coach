using System.Net;
using System.Text.Json;
using AIInterviewCoach.Tests.Common;

namespace AIInterviewCoach.Tests.Integration
{
    public sealed class ObservabilityIntegrationTests
    {
        [Fact]
        public async Task HealthEndpoints_ShouldExposeLiveAndReadyChecks()
        {
            await using var factory = new TestApiApplicationFactory();
            using var client = factory.CreateClient();

            var healthResponse = await client.GetAsync("/health");
            var liveResponse = await client.GetAsync("/health/live");
            var readyResponse = await client.GetAsync("/health/ready");

            Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, liveResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, readyResponse.StatusCode);

            using var readyPayload = JsonDocument.Parse(await readyResponse.Content.ReadAsStringAsync());
            Assert.Equal("Healthy", readyPayload.RootElement.GetProperty("status").GetString());
            Assert.True(
                readyPayload.RootElement.GetProperty("checks").TryGetProperty("database", out _),
                "Expected readiness payload to include the database check.");
        }

        [Fact]
        public async Task AdminObservabilityEndpoint_ShouldReturnSnapshotWithRuntimeMetrics()
        {
            await using var factory = new TestApiApplicationFactory();
            using var adminClient = await factory.CreateAuthenticatedClientAsync("admin@test.com");

            var replaceResponse = await adminClient.PostAsync("/api/problems/catalog/replace-with-starter-set", null);
            Assert.True(replaceResponse.IsSuccessStatusCode, await replaceResponse.Content.ReadAsStringAsync());

            var response = await adminClient.GetAsync("/api/admin/observability");
            Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());

            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = payload.RootElement;

            Assert.True(root.GetProperty("uptimeSeconds").GetDouble() >= 0);
            Assert.True(root.GetProperty("requests").GetProperty("totalRequests").GetInt64() >= 1);
            Assert.True(root.GetProperty("adminActions").GetProperty("totalActions").GetInt64() >= 1);

            var adminActions = root.GetProperty("adminActions").GetProperty("byActionType");
            Assert.Equal(1, adminActions.GetProperty("catalog.replaced").GetInt64());

            var routeNames = root.GetProperty("requests")
                .GetProperty("byRoute")
                .EnumerateObject()
                .Select(property => property.Name)
                .ToList();

            Assert.Contains(
                routeNames,
                routeName => routeName.Contains("replace-with-starter-set", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task AdminObservabilityEndpoint_ShouldRejectInterviewerAccess()
        {
            await using var factory = new TestApiApplicationFactory();
            using var interviewerClient = await factory.CreateAuthenticatedClientAsync("recruiter@test.com");

            var response = await interviewerClient.GetAsync("/api/admin/observability");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
