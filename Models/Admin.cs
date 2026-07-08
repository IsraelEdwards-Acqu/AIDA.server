namespace AIDA.Server.Models
{
    public class Admin
    {
        public int AdminId { get; set; }   // primary key
        public string Username { get; set; }   // ✅ add this
        public string PasswordHash { get; set; }   // hashed password
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
