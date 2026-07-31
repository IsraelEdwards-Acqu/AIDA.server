namespace AIDA.Server.Models
{
    public class KnowledgeEntry
    {
        public int Id { get; set; }   // Primary Key
        public int EntryId { get; set; }
        public string Question { get; set; }
        public string Answer { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}

