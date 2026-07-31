using AIDA.Server.Data;
using AIDA.Server.Models;
using AIDA.Server.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AIDA.Server.Services
{
    public class KnowledgeService
    {
        private readonly AidaDbContext _context;
        private readonly ILogger<KnowledgeService> _logger;

        public KnowledgeService(AidaDbContext context, ILogger<KnowledgeService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<string?> Search(string question)
        {
            if (string.IsNullOrWhiteSpace(question))
                return null;

            try
            {
                // Use case-insensitive search; EF.Functions.ILike works with PostgreSQL (Npgsql)
                var entry = await _context.KnowledgeBase
                    .AsNoTracking()
                    .FirstOrDefaultAsync(k => EF.Functions.ILike(k.Question, $"%{question}%"));

                return entry?.Answer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching knowledge base for query: {Query}", question);
                return null;
            }
        }

        public async Task<KnowledgeEntry> AddEntry(KnowledgeDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            var entry = new KnowledgeEntry
            {
                Question = dto.Question?.Trim() ?? string.Empty,
                Answer = dto.Answer?.Trim() ?? string.Empty,
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                _context.KnowledgeBase.Add(entry);
                await _context.SaveChangesAsync();
                return entry;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding knowledge entry: {Question}", dto.Question);
                throw;
            }
        }

        public async Task<List<KnowledgeEntry>> GetAllEntries()
        {
            try
            {
                return await _context.KnowledgeBase
                    .AsNoTracking()
                    .OrderByDescending(k => k.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all knowledge entries");
                return new List<KnowledgeEntry>();
            }
        }

        public async Task<bool> UpdateEntry(int id, KnowledgeDto dto)
        {
            if (id <= 0) return false;
            if (dto == null) return false;

            try
            {
                var entry = await _context.KnowledgeBase.FindAsync(id);
                if (entry == null) return false;

                entry.Question = dto.Question?.Trim() ?? entry.Question;
                entry.Answer = dto.Answer?.Trim() ?? entry.Answer;
                entry.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating knowledge entry id {Id}", id);
                return false;
            }
        }

        public async Task<bool> DeleteEntry(int id)
        {
            if (id <= 0) return false;

            try
            {
                var entry = await _context.KnowledgeBase.FindAsync(id);
                if (entry == null) return false;

                _context.KnowledgeBase.Remove(entry);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting knowledge entry id {Id}", id);
                return false;
            }
        }

        public async Task<KnowledgeEntry?> PromoteChat(int chatId)
        {
            if (chatId <= 0) return null;

            try
            {
                var chat = await _context.Chats.FindAsync(chatId);
                if (chat == null) return null;

                var entry = new KnowledgeEntry
                {
                    Question = chat.Message ?? string.Empty,
                    Answer = "Admin-provided answer",
                    CreatedAt = DateTime.UtcNow
                };

                _context.KnowledgeBase.Add(entry);
                await _context.SaveChangesAsync();
                return entry;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error promoting chat {ChatId} to knowledge entry", chatId);
                return null;
            }
        }
    }
}
