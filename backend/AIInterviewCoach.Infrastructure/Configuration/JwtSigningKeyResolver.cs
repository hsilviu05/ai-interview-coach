using System.Text;
using Microsoft.Extensions.Configuration;

namespace AIInterviewCoach.Infrastructure.Configuration
{
    public static class JwtSigningKeyResolver
    {
        private const int MinimumSigningKeyLengthInBytes = 32;

        public static string GetRequiredSigningKey(IConfiguration configuration)
        {
            var signingKey = configuration["Jwt:Key"];

            if (string.IsNullOrWhiteSpace(signingKey))
                throw new InvalidOperationException(
                    "JWT signing key is missing. Configure Jwt:Key via environment variables or dotnet user-secrets.");

            if (Encoding.UTF8.GetByteCount(signingKey) < MinimumSigningKeyLengthInBytes)
                throw new InvalidOperationException(
                    $"JWT signing key must be at least {MinimumSigningKeyLengthInBytes} bytes long.");

            return signingKey;
        }
    }
}
