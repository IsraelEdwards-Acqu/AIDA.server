using AIDA.Server.Data;
using AIDA.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace AIDA.Server.Services
{
    public class StudentService
    {
        private readonly AidaDbContext _context;

        public StudentService(AidaDbContext context)
        {
            _context = context;
        }

        public async Task<List<Student>> GetAllStudents()
        {
            return await _context.Students.ToListAsync();
        }

        public async Task<bool> ResetPassword(int studentId)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null) return false;

            student.PasswordHash = BCrypt.Net.BCrypt.HashPassword("Default123");
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteStudent(int studentId)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null) return false;

            _context.Students.Remove(student);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
