using AIDA.Server.DTOs;
using AIDA.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace AIDA.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableCors("AllowFrontend")]
    public class TicketsController : ControllerBase
    {
        private readonly TicketService _ticketService;
        private readonly ILogger<TicketsController> _logger;

        public TicketsController(TicketService ticketService, ILogger<TicketsController> logger)
        {
            _ticketService = ticketService;
            _logger = logger;
        }

        // Students (or Admin) create a ticket
        [HttpPost("create")]
        [Authorize]
        public async Task<IActionResult> CreateTicket([FromBody] TicketDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Request body is required." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // Validate caller: allow Admin or the student who owns the ticket
                var callerRole = User.FindFirst("role")?.Value ?? User.FindFirst(ClaimTypes.Role)?.Value;
                var callerIdClaim = User.FindFirst("studentId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!string.Equals(callerRole, "Admin", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrEmpty(callerIdClaim) || !int.TryParse(callerIdClaim, out var callerId) || callerId != dto.StudentId)
                    {
                        return Forbid();
                    }
                }

                var ticket = await _ticketService.CreateTicket(dto);
                return Ok(ticket);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ticket for student {StudentId}", dto?.StudentId);
                return StatusCode(500, new { message = "An error occurred while creating the ticket." });
            }
        }

        // Get tickets for a student (owner or admin)
        [HttpGet("{studentId}")]
        [Authorize]
        public async Task<IActionResult> GetTickets(int studentId)
        {
            try
            {
                var callerRole = User.FindFirst("role")?.Value ?? User.FindFirst(ClaimTypes.Role)?.Value;
                var callerIdClaim = User.FindFirst("studentId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!string.Equals(callerRole, "Admin", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrEmpty(callerIdClaim) || !int.TryParse(callerIdClaim, out var callerId) || callerId != studentId)
                    {
                        return Forbid();
                    }
                }

                var tickets = await _ticketService.GetTicketsByStudent(studentId);
                return Ok(tickets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching tickets for student {StudentId}", studentId);
                return StatusCode(500, new { message = "An error occurred while fetching tickets." });
            }
        }

        // Admin responds to a ticket
        [HttpPost("respond/{ticketId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Respond(int ticketId, [FromBody] AdminResponseDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Request body is required." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var response = await _ticketService.RespondToTicket(ticketId, dto);
                return Ok(response);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Ticket not found." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error responding to ticket {TicketId}", ticketId);
                return StatusCode(500, new { message = "An error occurred while responding to the ticket." });
            }
        }
    }
}
