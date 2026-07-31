using AIDA.Server.Data;
using AIDA.Server.DTOs;
using AIDA.Server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AIDA.Server.Services
{
    public class TicketService
    {
        private readonly AidaDbContext _context;
        private readonly ILogger<TicketService> _logger;

        public TicketService(AidaDbContext context, ILogger<TicketService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Ticket> CreateTicket(TicketDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            var ticket = new Ticket
            {
                StudentId = dto.StudentId,
                Category = dto.Category ?? string.Empty,
                Subject = dto.Subject ?? string.Empty,
                Status = "Open",
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                _context.Tickets.Add(ticket);
                await _context.SaveChangesAsync();
                return ticket;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ticket for student {StudentId}", dto.StudentId);
                throw;
            }
        }

        public async Task<List<Ticket>> GetTicketsByStudent(int studentId)
        {
            try
            {
                return await _context.Tickets
                    .AsNoTracking()
                    .Where(t => t.StudentId == studentId)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching tickets for student {StudentId}", studentId);
                return new List<Ticket>();
            }
        }

        public async Task<object> RespondToTicket(int ticketId, AdminResponseDto dto)
        {
            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket == null) throw new KeyNotFoundException("Ticket not found");

            // Only update status when provided
            if (!string.IsNullOrWhiteSpace(dto.Status))
            {
                ticket.Status = dto.Status;
            }

            ticket.UpdatedAt = DateTime.UtcNow;

            // Optionally persist admin response details if you have a responses table
            // Example: create AdminResponse entity and save it
            var response = new AdminResponse
            {
                TicketId = ticketId,
                AdminId = dto.AdminId,
                Message = dto.Message ?? string.Empty,
                CreatedAt = dto.CreatedAt
            };
            _context.Set<AdminResponse>().Add(response);

            await _context.SaveChangesAsync();

            return new { ticketId = ticket.TicketId, status = ticket.Status };
        }
        public async Task<List<Ticket>> GetAllTickets()
        {
            try
            {
                return await _context.Tickets
                    .AsNoTracking()
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all tickets");
                return new List<Ticket>();
            }
        }

        public async Task<bool> UpdateStatus(int id, string status)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null) return false;

            ticket.Status = status;
            ticket.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
