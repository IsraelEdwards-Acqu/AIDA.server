using AIDA.Server.Data;
using AIDA.Server.DTOs;
using AIDA.Server.Helpers;
using AIDA.Server.Models;

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

        public async Task<string> Signup(StudentSignupDto dto)
        {
            // Check if student already exists
            var existing = await _context.Students.FindAsync(dto.StudentId);
            if (existing != null)
            {
                return "Student already registered.";
            }

            var student = new Student
            {
                StudentId = dto.StudentId,
                Name = dto.Name,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
            };

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            return "Signup successful.";
        }

        // Updated: return LoginResult instead of string
        public async Task<LoginResult?> Login(StudentLoginDto dto)
        {
            var student = await _context.Students.FindAsync(dto.StudentId);
            if (student == null || !BCrypt.Net.BCrypt.Verify(dto.Password, student.PasswordHash))
            {
                return null; // invalid credentials
            }

            var key = _config["Jwt:Key"];
            var token = JwtHelper.GenerateToken(student.StudentId.ToString(), key);

            return new LoginResult
            {
                Token = token,
                StudentId = student.StudentId
            };
        }
    }

    // DTO for login response
    public class LoginResult
    {
        public string Token { get; set; }
        public int StudentId { get; set; }
    }
}
