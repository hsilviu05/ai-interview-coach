using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AIInterviewCoach.Application.Interfaces.Services;
using AIInterviewCoach.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AIInterviewCoach.Infrastructure.Services
{
    public class JwtTokenService : IJwtTokenService
    {
        private readonly IConfiguration _configuration;

        public JwtTokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateToken(User user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var signingKey = jwtSettings["Key"];

            if (string.IsNullOrWhiteSpace(signingKey))
                throw new InvalidOperationException(
                    "JWT signing key is missing. Configure Jwt:Key before generating tokens.");

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.Email, user.Email),
                new(ClaimTypes.Name, user.FullName),
                new(ClaimTypes.Role, user.UserRole.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
