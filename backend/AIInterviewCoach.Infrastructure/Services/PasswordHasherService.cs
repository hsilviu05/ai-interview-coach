using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using AIInterviewCoach.Application.Interfaces.Services;

namespace AIInterviewCoach.Infrastructure.Services
{
    public class PasswordHasherService : IPasswordHasherService
    {
        public string HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16);

            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 32));
            return $"{Convert.ToBase64String(salt)}.{hashed}";
        }

        public bool VerifyPassword(string password, string storedHash)
        {
            try
            {
                var parts = storedHash.Split('.');
                if (parts.Length != 2)
                    return false;

                byte[] salt = Convert.FromBase64String(parts[0]);
                byte[] hashOfInput = KeyDerivation.Pbkdf2(
                    password: password,
                    salt: salt,
                    prf: KeyDerivationPrf.HMACSHA256,
                    iterationCount: 10000,
                    numBytesRequested: 32);

                byte[] expectedHash = Convert.FromBase64String(parts[1]);
                return CryptographicOperations.FixedTimeEquals(hashOfInput, expectedHash);
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
