using AIDA.Server.DTOs;
using AIDA.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using Microsoft.Extensions.Logging;

namespace AIDA.Server.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/admin/tickets")]
    [EnableCors("AllowFrontend")]
    public class AdminTicketsController : ControllerBase
    {
        private readonly TicketService _ticketService;
        private readonly ILogger<AdminTicketsController> _logger;

        public AdminTicketsController(TicketService ticketService, ILogger<AdminTicketsController> logger)
        {
            _ticketService = ticketService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllTickets()
        {
            try
            {
                var tickets = await _ticketService.GetAllTickets();
                return Ok(tickets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching tickets");
                return StatusCode(500, new { message = "An error occurred while fetching tickets." });
            }
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Request body is required." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var success = await _ticketService.UpdateStatus(id, dto.Status);
                return success ? Ok(new { message = "Status updated" }) : NotFound(new { message = "Ticket not found." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for ticket id {TicketId}", id);
                return StatusCode(500, new { message = "An error occurred while updating ticket status." });
            }
        }
    }
}
