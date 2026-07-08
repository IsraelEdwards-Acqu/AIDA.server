namespace AIDA.Server.DTOs
{
    // DTOs/StudentSignupDto.cs
    public class StudentSignupDto
    {
        public int StudentId { get; set; }
        public string Name { get; set; }
        public string Password { get; set; }
        public string Email { get; set; } // ✅ add
    }
}
