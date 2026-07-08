namespace AIDA.Server.Models
{
    public class Student
    {
        public int StudentId { get; set; }
        public string Name { get; set; }
        public string PasswordHash { get; set; }
        public string Email { get; set; } // ✅ add
        public DateTime CreatedAt { get; set; }
    }
}
