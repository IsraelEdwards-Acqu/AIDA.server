using AIDA.Server.DTOs;
using AIDA.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace AIDA.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TicketsController : ControllerBase
    {
        private readonly TicketService _ticketService;

        public TicketsController(TicketService ticketService)
        {
            _ticketService = ticketService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateTicket(TicketDto dto)
        {
            var ticket = await _ticketService.CreateTicket(dto);
            return Ok(ticket);
        }

        [HttpGet("{studentId}")]
        public async Task<IActionResult> GetTickets(int studentId)
        {
            var tickets = await _ticketService.GetTicketsByStudent(studentId);
            return Ok(tickets);
        }

        [HttpPost("respond/{ticketId}")]
        public async Task<IActionResult> Respond(int ticketId, AdminResponseDto dto)
        {
            var response = await _ticketService.RespondToTicket(ticketId, dto);
            return Ok(response);
        }
    }

}
