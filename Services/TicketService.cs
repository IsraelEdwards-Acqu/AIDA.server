using AIDA.Server.Data;
using AIDA.Server.Models;
using AIDA.Server.DTOs;
using Microsoft.EntityFrameworkCore;

namespace AIDA.Server.Services
{
    public class TicketService
    {
        private readonly AidaDbContext _context;

        public TicketService(AidaDbContext context)
        {
            _context = context;
        }

        public async Task<Ticket> CreateTicket(TicketDto dto)
        {
            var ticket = new Ticket
            {
                StudentId = dto.StudentId,
                Category = dto.Category,
                Subject = dto.Subject,
                Status = "Open"
            };
            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();
            return ticket;
        }

        public async Task<List<Ticket>> GetTicketsByStudent(int studentId) =>
            await _context.Tickets.Where(t => t.StudentId == studentId).ToListAsync();

        public async Task<Models.AdminResponse> RespondToTicket(int ticketId, AdminResponseDto dto)
        {
            var response = new Models.AdminResponse
            {
                TicketId = ticketId,
                AdminId = dto.AdminId,
                Message = dto.Message
            };
            _context.AdminResponses.Add(response);

            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket != null)
            {
                ticket.Status = "Resolved";
                ticket.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return response;
        }
    }
}
