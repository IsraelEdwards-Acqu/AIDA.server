using AIDA.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/admin/students")]
public class AdminStudentsController : ControllerBase
{
    private readonly StudentService _studentService;

    public AdminStudentsController(StudentService studentService)
    {
        _studentService = studentService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllStudents()
    {
        var students = await _studentService.GetAllStudents();
        return Ok(students);
    }

    // ✅ Updated to accept int
    [HttpPut("{studentId}/reset-password")]
    public async Task<IActionResult> ResetPassword(int studentId)
    {
        var success = await _studentService.ResetPassword(studentId);
        if (!success) return NotFound();
        return Ok(new { message = "Password reset successfully" });
    }

    // ✅ Updated to accept int
    [HttpDelete("{studentId}")]
    public async Task<IActionResult> DeleteStudent(int studentId)
    {
        var success = await _studentService.DeleteStudent(studentId);
        if (!success) return NotFound();
        return Ok(new { message = "Student deleted successfully" });
    }
}
