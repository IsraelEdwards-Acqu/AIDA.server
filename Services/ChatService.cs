using AIDA.Server.Data;
using AIDA.Server.Models;
using AIDA.Server.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AIDA.Server.Services
{
    /// <summary>
    /// ChatService handles storing user messages, querying the knowledge base,
    /// creating bot replies, and escalating conversations into support tickets.
    /// </summary>
    public class ChatService
    {
        private readonly AidaDbContext _context;
        private readonly KnowledgeService _knowledgeService;
        private readonly ILogger<ChatService> _logger;

        public ChatService(
            AidaDbContext context,
            KnowledgeService knowledgeService,
            ILogger<ChatService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _knowledgeService = knowledgeService ?? throw new ArgumentNullException(nameof(knowledgeService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> ProcessMessage(ChatDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (string.IsNullOrWhiteSpace(dto.Message)) return "Message cannot be empty.";

            try
            {
                var chat = new Chat
                {
                    StudentId = dto.StudentId,
                    Message = dto.Message,
                    Sender = SenderType.User.ToString(),
                    CreatedAt = DateTime.UtcNow
                };

                _context.Chats.Add(chat);
                await _context.SaveChangesAsync();

                var kbAnswer = await _knowledgeService.Search(dto.Message);
                if (!string.IsNullOrWhiteSpace(kbAnswer))
                {
                    var botReply = new Chat
                    {
                        StudentId = dto.StudentId,
                        Message = kbAnswer,
                        Sender = SenderType.Bot.ToString(),
                        CreatedAt = DateTime.UtcNow,
                        Metadata = "Source:KnowledgeBase"
                    };

                    _context.Chats.Add(botReply);
                    await _context.SaveChangesAsync();

                    return kbAnswer;
                }

                var escalationPlaceholder = new Chat
                {
                    StudentId = dto.StudentId,
                    Message = "I’m not sure. Escalating to admin...",
                    Sender = SenderType.Bot.ToString(),
                    CreatedAt = DateTime.UtcNow,
                    Metadata = "Source:EscalationSuggested"
                };

                _context.Chats.Add(escalationPlaceholder);
                await _context.SaveChangesAsync();

                return "I’m not sure. Escalating to admin...";
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error while processing message for StudentId {StudentId}", dto.StudentId);
                return "An error occurred while processing your message. Please try again later.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while processing message for StudentId {StudentId}", dto.StudentId);
                return "An unexpected error occurred. Please try again later.";
            }
        }

        public async Task<List<Chat>> GetChatHistory(int studentId) =>
            await _context.Chats
                .Where(c => c.StudentId == studentId)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();

        public async Task<List<ChatMessageDto>> GetChatHistory(int studentId, int page = 1, int pageSize = 100)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 100;

            var skip = (page - 1) * pageSize;

            var chats = await _context.Chats
                .Where(c => c.StudentId == studentId)
                .OrderBy(c => c.CreatedAt)
                .Skip(skip)
                .Take(pageSize)
                .Select(c => new ChatMessageDto
                {
                    Id = c.Id,
                    StudentId = c.StudentId,
                    Message = c.Message,
                    Sender = c.Sender,
                    CreatedAt = c.CreatedAt,
                    Metadata = c.Metadata
                })
                .ToListAsync();

            return chats;
        }

        public async Task<Ticket> EscalateToTicket(ChatDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (string.IsNullOrWhiteSpace(dto.Message)) throw new ArgumentException("Message cannot be empty.", nameof(dto));

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var ticket = new Ticket
                {
                    StudentId = dto.StudentId,
                    Category = "Bot Escalation",
                    Subject = dto.Message.Length <= 200 ? dto.Message : dto.Message[..200],
                    Status = TicketStatus.Open.ToString(),
                    CreatedAt = DateTime.UtcNow
                };

                _context.Tickets.Add(ticket);
                await _context.SaveChangesAsync();

                var originatingChat = await _context.Chats
                    .Where(c => c.StudentId == dto.StudentId && c.Message == dto.Message)
                    .OrderByDescending(c => c.CreatedAt)
                    .FirstOrDefaultAsync();

                if (originatingChat != null)
                {
                    var ticketNote = new TicketMessage
                    {
                        TicketId = ticket.TicketId,
                        Author = "System",
                        Message = $"Escalated from chat (ChatId: {originatingChat.Id})",
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Set<TicketMessage>().Add(ticketNote);

                    originatingChat.Metadata = (originatingChat.Metadata ?? string.Empty) + $"|EscalatedToTicket:{ticket.TicketId}";
                    _context.Chats.Update(originatingChat);
                }
                else
                {
                    var ticketNote = new TicketMessage
                    {
                        TicketId = ticket.TicketId,
                        Author = "System",
                        Message = $"Escalated message: {dto.Message}",
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Set<TicketMessage>().Add(ticketNote);
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                _logger.LogInformation("Created ticket {TicketId} for StudentId {StudentId}", ticket.TicketId, dto.StudentId);

                return ticket;
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error while escalating message for StudentId {StudentId}", dto.StudentId);
                await tx.RollbackAsync();
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while escalating message for StudentId {StudentId}", dto.StudentId);
                await tx.RollbackAsync();
                throw;
            }
        }
    }

    #region Supporting types

    public enum SenderType
    {
        User,
        Bot,
        System
    }

    public class ChatMessageDto
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Sender { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? Metadata { get; set; }
    }

    public class TicketMessage
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public string Author { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public enum TicketStatus
    {
        Open,
        InProgress,
        Resolved,
        Closed
    }

    #endregion
}
