using Microsoft.Extensions.DependencyInjection;

namespace AIInterviewCoach.Tests.Common
{
    /// <summary>
    /// Minimal hand-rolled IServiceScopeFactory seam. Each CreateScope() invocation
    /// delegates to the provided factory, allowing tests to return different providers
    /// per scope (e.g. to inject failures on a specific call).
    /// </summary>
    public sealed class FakeServiceScopeFactory : IServiceScopeFactory
    {
        private readonly Func<IServiceProvider> _providerFactory;

        public FakeServiceScopeFactory(IServiceProvider provider)
            : this(() => provider) { }

        public FakeServiceScopeFactory(Func<IServiceProvider> providerFactory)
        {
            _providerFactory = providerFactory;
        }

        public IServiceScope CreateScope() => new FakeServiceScope(_providerFactory());

        private sealed class FakeServiceScope : IServiceScope
        {
            public FakeServiceScope(IServiceProvider provider) => ServiceProvider = provider;
            public IServiceProvider ServiceProvider { get; }
            public void Dispose() { }
        }
    }

    /// <summary>
    /// Dictionary-backed IServiceProvider. Factory delegates are invoked on each
    /// GetService call, so tests can control the returned instance per-call.
    /// Returns null (not throws) for unregistered types; GetRequiredService() then
    /// converts null to the standard InvalidOperationException.
    /// </summary>
    public sealed class FakeServiceProvider : IServiceProvider
    {
        private readonly Dictionary<Type, Func<object>> _services;

        public FakeServiceProvider(Dictionary<Type, Func<object>> services)
        {
            _services = services;
        }

        public object? GetService(Type serviceType) =>
            _services.TryGetValue(serviceType, out var factory) ? factory() : null;
    }
}
