namespace AIDA.Server.Models
{
    public class Student
    {
        // Primary key used across the app
        public int StudentId { get; set; }

        // Display fields
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        // Stored hashed password
        public string PasswordHash { get; set; } = string.Empty;

        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
