namespace AIDA.Server.DTOs
{
    public class ChatDto
    {
        public int StudentId { get; set; }
        public string Message { get; set; }

        // New property for bilingual support
        public string Language { get; set; } = "en";
    }
}
