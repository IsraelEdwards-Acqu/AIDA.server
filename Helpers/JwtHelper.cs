using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AIDA.Server.Helpers
{
    public static class JwtHelper
    {
        /// <summary>
        /// Generate a JWT token including standard name identifier and role claims.
        /// Optional extra claims can be provided (e.g., studentId, adminId).
        /// Default expiration is 12 hours to match server auth behavior.
        /// </summary>
        public static string GenerateToken(string userId, string role, string key, IEnumerable<Claim>? extraClaims = null, TimeSpan? expires = null)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenKey = Encoding.UTF8.GetBytes(key);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, role),
                // include a simple "role" claim for clients that read custom claims
                new Claim("role", role)
            };

            if (extraClaims != null)
            {
                claims.AddRange(extraClaims);
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.Add(expires ?? TimeSpan.FromHours(12)),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(tokenKey),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
