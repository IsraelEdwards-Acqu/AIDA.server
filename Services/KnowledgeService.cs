using AIDA.Server.Data;
using AIDA.Server.Models;
using AIDA.Server.DTOs;
using Microsoft.EntityFrameworkCore;

namespace AIDA.Server.Services
{
    public class KnowledgeService
    {
        private readonly AidaDbContext _context;

        public KnowledgeService(AidaDbContext context)
        {
            _context = context;
        }

        public async Task<string?> Search(string question)
        {
            var entry = await _context.KnowledgeBase
                .FirstOrDefaultAsync(k => k.Question.Contains(question));
            return entry?.Answer;
        }

        public async Task<KnowledgeEntry> AddEntry(KnowledgeDto dto)
        {
            var entry = new KnowledgeEntry
            {
                Question = dto.Question,
                Answer = dto.Answer
            };
            _context.KnowledgeBase.Add(entry);
            await _context.SaveChangesAsync();
            return entry;
        }

        // ✅ New: Get all entries
        public async Task<List<KnowledgeEntry>> GetAllEntries()
        {
            return await _context.KnowledgeBase.ToListAsync();
        }

        // ✅ New: Update entry
        public async Task<bool> UpdateEntry(int id, KnowledgeDto dto)
        {
            var entry = await _context.KnowledgeBase.FindAsync(id);
            if (entry == null) return false;

            entry.Question = dto.Question;
            entry.Answer = dto.Answer;
            await _context.SaveChangesAsync();
            return true;
        }

        // ✅ New: Delete entry
        public async Task<bool> DeleteEntry(int id)
        {
            var entry = await _context.KnowledgeBase.FindAsync(id);
            if (entry == null) return false;

            _context.KnowledgeBase.Remove(entry);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<KnowledgeEntry?> PromoteChat(int chatId)
        {
            var chat = await _context.Chats.FindAsync(chatId);
            if (chat == null) return null;

            var entry = new KnowledgeEntry
            {
                Question = chat.Message,
                Answer = "Admin-provided answer"
            };
            _context.KnowledgeBase.Add(entry);
            await _context.SaveChangesAsync();
            return entry;
        }
    }
}