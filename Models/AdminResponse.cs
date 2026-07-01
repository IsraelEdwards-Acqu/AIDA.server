namespace AIDA.Server.Models
{
    public class AdminResponse
    {
        public int Id { get; set; }   // Primary Key
        public int TicketId { get; set; }
        public int AdminId { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
