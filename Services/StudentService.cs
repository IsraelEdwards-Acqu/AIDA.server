using AIDA.Server.Data;
using AIDA.Server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace AIDA.Server.Services
{
    public class StudentService
    {
        private readonly AidaDbContext _context;
        private readonly ILogger<StudentService> _logger;

        public StudentService(AidaDbContext context, ILogger<StudentService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Student>> GetAllStudents()
        {
            try
            {
                return await _context.Students
                    .AsNoTracking()
                    .OrderBy(s => s.StudentId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving students list");
                return new List<Student>();
            }
        }
        /// <summary>
        /// Generate a secure temporary password, set the student's password hash to it,
        /// update UpdatedAt, and return the plaintext temporary password so it can be delivered to the student.
        /// Returns null on failure.
        /// </summary>
        public async Task<string?> ResetPassword(int studentId)
        {
            if (studentId <= 0)
            {
                _logger.LogWarning("ResetPassword called with invalid studentId {StudentId}", studentId);
                return null;
            }

            try
            {
                var student = await _context.Students.FindAsync(studentId);
                if (student == null)
                {
                    _logger.LogInformation("ResetPassword: student not found {StudentId}", studentId);
                    return null;
                }

                // Generate a secure temporary password
                var tempPassword = GenerateSecurePassword(12);

                // Hash and store
                student.PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword);
                student.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Password reset for student {StudentId}", studentId);

                // Return the plaintext temporary password so the caller can deliver it securely
                return tempPassword;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for student {StudentId}", studentId);
                return null;
            }
        }

        public async Task<bool> DeleteStudent(int studentId)
        {
            if (studentId <= 0)
            {
                _logger.LogWarning("DeleteStudent called with invalid studentId {StudentId}", studentId);
                return false;
            }

            try
            {
                var student = await _context.Students.FindAsync(studentId);
                if (student == null)
                {
                    _logger.LogInformation("DeleteStudent: student not found {StudentId}", studentId);
                    return false;
                }

                _context.Students.Remove(student);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted student {StudentId}", studentId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting student {StudentId}", studentId);
                return false;
            }
        }

        // Helper: secure random password generator
        private static string GenerateSecurePassword(int length = 12)
        {
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ"; // removed ambiguous chars
            const string lower = "abcdefghijkmnopqrstuvwxyz";
            const string digits = "23456789";
            const string symbols = "!@#$%&*?";

            var allChars = upper + lower + digits + symbols;
            var passwordChars = new char[length];

            // Ensure at least one of each required type
            using var rng = RandomNumberGenerator.Create();
            passwordChars[0] = upper[GetRandomInt(rng, upper.Length)];
            passwordChars[1] = lower[GetRandomInt(rng, lower.Length)];
            passwordChars[2] = digits[GetRandomInt(rng, digits.Length)];
            passwordChars[3] = symbols[GetRandomInt(rng, symbols.Length)];

            for (int i = 4; i < length; i++)
            {
                passwordChars[i] = allChars[GetRandomInt(rng, allChars.Length)];
            }

            // Shuffle to avoid predictable positions
            return Shuffle(passwordChars, rng);
        }

        private static int GetRandomInt(RandomNumberGenerator rng, int maxExclusive)
        {
            var buffer = new byte[4];
            rng.GetBytes(buffer);
            var value = BitConverter.ToUInt32(buffer, 0);
            return (int)(value % (uint)maxExclusive);
        }

        private static string Shuffle(char[] array, RandomNumberGenerator rng)
        {
            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = GetRandomInt(rng, i + 1);
                var tmp = array[i];
                array[i] = array[j];
                array[j] = tmp;
            }
            return new string(array);
        }
    }
}
