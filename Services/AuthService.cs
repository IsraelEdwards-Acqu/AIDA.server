using AIDA.Server.Data;
using AIDA.Server.DTOs;
using AIDA.Server.Helpers;
using AIDA.Server.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace AIDA.Server.Services
{
    public class AuthService
    {
        private readonly AidaDbContext _context;
        private readonly IConfiguration _config;

        public AuthService(AidaDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // Student signup
        public async Task<string> Signup(StudentSignupDto dto)
        {
            var existing = await _context.Students.FindAsync(dto.StudentId);
            if (existing != null)
            {
                return "Student already registered.";
            }

            var student = new Student
            {
                StudentId = dto.StudentId,
                Name = dto.Name,
                Email = dto.Email,
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
            var student = await _context.Students.FindAsync(dto.StudentId);
            if (student == null || !BCrypt.Net.BCrypt.Verify(dto.Password, student.PasswordHash))
            {
                return null;
            }

            var key = _config["Jwt:Key"];
            var token = GenerateJwtToken(student.StudentId.ToString(), "Student", key);

            return new LoginResult
            {
                Token = token,
                StudentId = student.StudentId
            };
        }

        // ✅ Admin login
        public async Task<LoginResult?> AdminLogin(AdminLoginDto dto)
        {
            // For simplicity, check against a table or config
            var admin = await _context.Admins
                .FirstOrDefaultAsync(a => a.Username == dto.Username);

            if (admin == null || !BCrypt.Net.BCrypt.Verify(dto.Password, admin.PasswordHash))
            {
                return null;
            }

            var key = _config["Jwt:Key"];
            var token = GenerateJwtToken(admin.Username, "Admin", key);

            return new LoginResult
            {
                Token = token,
                StudentId = 0 // not applicable for admin
            };
        }

        // Helper: generate JWT with role claim
        private string GenerateJwtToken(string userId, string role, string key)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var keyBytes = Encoding.UTF8.GetBytes(key);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, role)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(12),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(keyBytes),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }

    // DTO for login response
    public class LoginResult
    {
        public string Token { get; set; }
        public int StudentId { get; set; }
    }
}