namespace AIDA.Server.Models
{
    public class Ticket
    {
        public int TicketId { get; set; }
        public int StudentId { get; set; }
        public string Category { get; set; }
        public string Subject { get; set; }
        public string Status { get; set; } = "Open"; // Open/InProgress/Resolved
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
