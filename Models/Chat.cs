namespace AIDA.Server.Models
{
    public class Chat
    {
        // Primary key
        public int Id { get; set; }

        // Foreign key to Student
        public int StudentId { get; set; }

        // Message text
        public string Message { get; set; } = string.Empty;

        // Sender: "User", "Bot", "System"
        public string Sender { get; set; } = string.Empty;

        // When the message was created (UTC)
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Optional metadata (source, tags, references)
        public string? Metadata { get; set; }
    }
}
