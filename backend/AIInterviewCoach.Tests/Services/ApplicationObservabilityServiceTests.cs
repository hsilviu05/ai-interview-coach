using AIInterviewCoach.Infrastructure.Services;

namespace AIInterviewCoach.Tests.Services
{
    public class ApplicationObservabilityServiceTests
    {
        [Fact]
        public void RecordRequest_IncrementsFailedRequests_WhenStatusCodeIs500()
        {
            var service = new ApplicationObservabilityService();

            service.RecordRequest("GET", "/api/test", 500, TimeSpan.Zero);

            var snapshot = service.GetSnapshot();
            Assert.Equal(1, snapshot.Requests.TotalRequests);
            Assert.Equal(1, snapshot.Requests.FailedRequests);
        }

        [Fact]
        public void RecordRequest_DoesNotIncrementFailedRequests_WhenStatusCodeIs200()
        {
            var service = new ApplicationObservabilityService();

            service.RecordRequest("GET", "/api/test", 200, TimeSpan.Zero);

            var snapshot = service.GetSnapshot();
            Assert.Equal(1, snapshot.Requests.TotalRequests);
            Assert.Equal(0, snapshot.Requests.FailedRequests);
        }

        [Fact]
        public void RecordRequest_NormalizesWhitespaceOnlyMethod_ToUnknown()
        {
            var service = new ApplicationObservabilityService();

            service.RecordRequest("   ", "/api/test", 200, TimeSpan.Zero);

            var snapshot = service.GetSnapshot();
            Assert.True(snapshot.Requests.ByMethod.ContainsKey("UNKNOWN"));
        }

        [Fact]
        public void RecordRequest_NormalizesWhitespaceOnlyRoute_ToUnknown()
        {
            var service = new ApplicationObservabilityService();

            service.RecordRequest("GET", "   ", 200, TimeSpan.Zero);

            var snapshot = service.GetSnapshot();
            Assert.True(snapshot.Requests.ByRoute.ContainsKey("unknown"));
        }
    }
}
