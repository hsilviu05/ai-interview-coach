using System.Net;
using System.Text;

namespace AIInterviewCoach.Tests.Common
{
    public sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

        public List<HttpRequestMessage> Requests { get; } = [];

        public FakeHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
            : this(req => Task.FromResult(handler(req)))
        {
        }

        public static FakeHttpMessageHandler Returning(
            string jsonBody,
            HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            return new FakeHttpMessageHandler(_ =>
                new HttpResponseMessage(statusCode)
                {
                    Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
                });
        }

        public static FakeHttpMessageHandler Throwing(Exception exception) =>
            new FakeHttpMessageHandler(_ => Task.FromException<HttpResponseMessage>(exception));

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return _handler(request);
        }
    }
}
