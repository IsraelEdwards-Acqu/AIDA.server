namespace AIDA.Server.DTOs
{
    // DTOs/AdminResponseDto.cs
    public class AdminResponseDto
    {
        public int Id { get; set; }   // Primary Key
        public int TicketId { get; set; }
        public int AdminId { get; set; }
        public string Message { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

}
