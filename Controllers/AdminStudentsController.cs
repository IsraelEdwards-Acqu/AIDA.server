using AIDA.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using Microsoft.Extensions.Logging;

namespace AIDA.Server.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/admin/students")]
    [EnableCors("AllowFrontend")]
    public class AdminStudentsController : ControllerBase
    {
        private readonly StudentService _studentService;
        private readonly ILogger<AdminStudentsController> _logger;

        public AdminStudentsController(StudentService studentService, ILogger<AdminStudentsController> logger)
        {
            _studentService = studentService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllStudents()
        {
            try
            {
                var students = await _studentService.GetAllStudents();
                return Ok(students);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching students list");
                return StatusCode(500, new { message = "An error occurred while fetching students." });
            }
        }

        [HttpPut("{studentId}/reset-password")]
        public async Task<IActionResult> ResetPassword(int studentId)
        {
            if (studentId <= 0)
                return BadRequest(new { message = "Invalid studentId." });

            try
            {
                // StudentService.ResetPassword returns a plaintext temporary password or null on failure
                var temporaryPassword = await _studentService.ResetPassword(studentId);

                if (temporaryPassword == null)
                    return NotFound(new { message = "Student not found or reset failed." });

                // Return the temporary password so the caller can deliver it securely.
                // Do not log the password.
                return Ok(new { temporaryPassword });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for studentId {StudentId}", studentId);
                return StatusCode(500, new { message = "An error occurred while resetting the password." });
            }
        }

        [HttpDelete("{studentId}")]
        public async Task<IActionResult> DeleteStudent(int studentId)
        {
            if (studentId <= 0)
                return BadRequest(new { message = "Invalid studentId." });

            try
            {
                var success = await _studentService.DeleteStudent(studentId);
                if (!success) return NotFound(new { message = "Student not found." });
                return Ok(new { message = "Student deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting student with id {StudentId}", studentId);
                return StatusCode(500, new { message = "An error occurred while deleting the student." });
            }
        }
    }
}
