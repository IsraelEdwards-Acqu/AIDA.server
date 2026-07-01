using AIDA.Server.Data;
using AIDA.Server.Models;
using AIDA.Server.DTOs;
using Microsoft.EntityFrameworkCore;

namespace AIDA.Server.Services
{
    public class ChatService
    {
        private readonly AidaDbContext _context;
        private readonly KnowledgeService _knowledgeService;

        public ChatService(AidaDbContext context, KnowledgeService knowledgeService)
        {
            _context = context;
            _knowledgeService = knowledgeService;
        }

        public async Task<string> ProcessMessage(ChatDto dto)
        {
            var chat = new Chat
            {
                StudentId = dto.StudentId,
                Message = dto.Message,
                Sender = "User"
            };
            _context.Chats.Add(chat);
            await _context.SaveChangesAsync();

            var kbAnswer = await _knowledgeService.Search(dto.Message);
            if (!string.IsNullOrEmpty(kbAnswer))
            {
                var botReply = new Chat
                {
                    StudentId = dto.StudentId,
                    Message = kbAnswer,
                    Sender = "Bot"
                };
                _context.Chats.Add(botReply);
                await _context.SaveChangesAsync();
                return kbAnswer;
            }

            return "I’m not sure. Escalating to admin...";
        }

        public async Task<List<Chat>> GetChatHistory(int studentId) =>
            await _context.Chats.Where(c => c.StudentId == studentId).ToListAsync();

        public async Task<Ticket> EscalateToTicket(ChatDto dto)
        {
            var ticket = new Ticket
            {
                StudentId = dto.StudentId,
                Category = "Bot Escalation",
                Subject = dto.Message,
                Status = "Open"
            };
            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();
            return ticket;
        }
    }
}
