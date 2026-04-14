using AIInterviewCoach.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;

namespace AIInterviewCoach.Tests.Services
{
    public class JwtSigningKeyResolverTests
    {
        [Fact]
        public void GetRequiredSigningKey_ShouldThrow_WhenKeyIsMissing()
        {
            var configuration = BuildConfiguration(null);

            var exception = Assert.Throws<InvalidOperationException>(() =>
                JwtSigningKeyResolver.GetRequiredSigningKey(configuration));

            Assert.Contains("JWT signing key is missing", exception.Message);
        }

        [Fact]
        public void GetRequiredSigningKey_ShouldThrow_WhenKeyIsTooShort()
        {
            var configuration = BuildConfiguration("short-signing-key");

            var exception = Assert.Throws<InvalidOperationException>(() =>
                JwtSigningKeyResolver.GetRequiredSigningKey(configuration));

            Assert.Contains("at least 32 bytes", exception.Message);
        }

        [Fact]
        public void GetRequiredSigningKey_ShouldReturnKey_WhenKeyIsLongEnough()
        {
            const string signingKey = "12345678901234567890123456789012";
            var configuration = BuildConfiguration(signingKey);

            var resolvedKey = JwtSigningKeyResolver.GetRequiredSigningKey(configuration);

            Assert.Equal(signingKey, resolvedKey);
        }

        private static IConfiguration BuildConfiguration(string? signingKey)
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = signingKey,
                })
                .Build();
        }
    }
}
