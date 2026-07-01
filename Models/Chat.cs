namespace AIDA.Server.Models
{
    public class Chat
    {
        public int ChatId { get; set; }
        public int StudentId { get; set; }
        public string Message { get; set; }
        public string Sender { get; set; } // "User", "Bot", "Admin"
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
