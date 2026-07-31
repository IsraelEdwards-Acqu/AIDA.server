namespace AIDA.Server.DTOs
{
    // DTOs/AdminResponseDto.cs
    public class AdminResponseDto
    {
        public int Id { get; set; }   // Primary Key (if used)
        public int TicketId { get; set; }
        public int AdminId { get; set; }
        public string? Status { get; set; }      // NEW: optional status to update ticket
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
