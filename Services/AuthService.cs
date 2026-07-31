using AIDA.Server.Data;
using AIDA.Server.DTOs;
using AIDA.Server.Helpers;
using AIDA.Server.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AIDA.Server.Services
{
    public class AuthService
    {
        private readonly AidaDbContext _context;
        private readonly IConfiguration _config;

        public AuthService(AidaDbContext context, IConfiguration config)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        // Student signup
        public async Task<string> Signup(StudentSignupDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (dto.StudentId <= 0) return "Invalid student id.";
            if (string.IsNullOrWhiteSpace(dto.Password)) return "Password is required.";

            // Check existing by primary key
            var existing = await _context.Students.FindAsync(dto.StudentId);
            if (existing != null)
            {
                return "Student already registered.";
            }

            var student = new Student
            {
                StudentId = dto.StudentId,
                Name = dto.Name?.Trim() ?? string.Empty,
                Email = dto.Email?.Trim() ?? string.Empty,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                CreatedAt = DateTime.UtcNow
            };

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            return "Signup successful.";
        }

        // Student login
        public async Task<LoginResult?> Login(StudentLoginDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (dto.StudentId <= 0 || string.IsNullOrWhiteSpace(dto.Password)) return null;

            // Use FindAsync for primary key lookup
            var student = await _context.Students.FindAsync(dto.StudentId);
            if (student == null) return null;

            var verified = false;
            try
            {
                verified = BCrypt.Net.BCrypt.Verify(dto.Password, student.PasswordHash);
            }
            catch
            {
                // If hashing verification throws, treat as failed auth
                verified = false;
            }

            if (!verified) return null;

            var key = _config["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(key)) throw new InvalidOperationException("JWT key not configured.");

            var expires = DateTime.UtcNow.AddHours(12);

            // include student id claim and role claim
            var token = GenerateJwtToken(
                userId: student.StudentId.ToString(),
                role: "Student",
                key: key,
                expires: expires,
                extraClaims: new[]
                {
                    new Claim("studentId", student.StudentId.ToString())
                });

            return new LoginResult
            {
                Token = token,
                Role = "Student",
                UserId = student.StudentId.ToString(),
                StudentId = student.StudentId,
                ExpiresAt = expires
            };
        }

        // Admin login
        // Admin login (replace existing method body with this)
        // Replace the existing AdminLogin method with this
        public async Task<LoginResult?> AdminLogin(AdminLoginDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password)) return null;

            // Diagnostic log (safe: do not log password)
            Console.WriteLine($"[AuthService] AdminLogin called for Username='{dto.Username}' at {DateTime.UtcNow:O}");

            try
            {
                var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Username == dto.Username);
                if (admin == null)
                {
                    Console.WriteLine($"[AuthService] AdminLogin: admin not found for '{dto.Username}'");
                    return null;
                }

                if (string.IsNullOrWhiteSpace(admin.PasswordHash))
                {
                    Console.WriteLine($"[AuthService] AdminLogin: admin record has empty PasswordHash for '{dto.Username}' (adminId={admin.AdminId})");
                    return null;
                }

                var verified = false;
                try
                {
                    verified = BCrypt.Net.BCrypt.Verify(dto.Password, admin.PasswordHash);
                }
                catch (Exception exVerify)
                {
                    Console.WriteLine($"[AuthService] AdminLogin: BCrypt.Verify threw for '{dto.Username}': {exVerify.GetType().Name} - {exVerify.Message}");
                    verified = false;
                }

                if (!verified)
                {
                    Console.WriteLine($"[AuthService] AdminLogin: invalid credentials for '{dto.Username}'");
                    return null;
                }

                var key = _config["Jwt:Key"];
                if (string.IsNullOrWhiteSpace(key))
                {
                    Console.WriteLine("[AuthService] AdminLogin: Jwt:Key missing in configuration");
                    throw new InvalidOperationException("JWT key not configured.");
                }

                var expires = DateTime.UtcNow.AddHours(12);
                var adminIdString = admin.AdminId.ToString();

                var token = GenerateJwtToken(
                    userId: adminIdString,
                    role: "Admin",
                    key: key,
                    expires: expires,
                    extraClaims: new[] { new Claim("adminId", adminIdString) });

                Console.WriteLine($"[AuthService] AdminLogin: success for '{dto.Username}', adminId={adminIdString}");
                return new LoginResult
                {
                    Token = token,
                    Role = "Admin",
                    UserId = adminIdString,
                    StudentId = null,
                    ExpiresAt = expires
                };
            }
            catch (Exception ex)
            {
                // Full exception to console so Render captures stack trace
                Console.WriteLine($"[AuthService] AdminLogin: exception for '{dto?.Username}': {ex}");
                throw;
            }
        }
        // Helper: generate JWT with role claim and optional extra claims
        private string GenerateJwtToken(string userId, string role, string key, DateTime? expires = null, IEnumerable<Claim>? extraClaims = null)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var keyBytes = Encoding.UTF8.GetBytes(key);

            var now = DateTime.UtcNow;
            var expiry = expires ?? now.AddHours(12);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, role),
                new Claim("role", role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            if (extraClaims != null)
            {
                claims.AddRange(extraClaims);
            }

            var signingKey = new SymmetricSecurityKey(keyBytes);
            var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                IssuedAt = now,
                NotBefore = now,
                Expires = expiry,
                SigningCredentials = signingCredentials
            };

            // Optionally set issuer/audience if configured
            var issuer = _config["Jwt:Issuer"];
            var audience = _config["Jwt:Audience"];
            if (!string.IsNullOrWhiteSpace(issuer))
            {
                tokenDescriptor.Issuer = issuer;
            }
            if (!string.IsNullOrWhiteSpace(audience))
            {
                tokenDescriptor.Audience = audience;
            }

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }

    // DTO for login response
    public class LoginResult
    {
        /// <summary>
        /// JWT access token
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Primary identifier for the authenticated user (string form).
        /// For students this will be the StudentId.ToString(); for admins the admin Id.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// When applicable (student login) contains the numeric StudentId; otherwise null.
        /// </summary>
        public int? StudentId { get; set; }

        /// <summary>
        /// Role assigned to the token (e.g., "Student", "Admin").
        /// </summary>
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// UTC expiry time for the token.
        /// </summary>
        public DateTime ExpiresAt { get; set; }
    }
}
