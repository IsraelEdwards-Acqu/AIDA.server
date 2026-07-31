using AIDA.Server.DTOs;
using AIDA.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AIDA.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableCors("AllowFrontend")]
    public class ChatController : ControllerBase
    {
        private readonly ChatService _chatService;
        private readonly TranslationService _translator;
        private readonly ILogger<ChatController> _logger;

        public ChatController(ChatService chatService, TranslationService translator, ILogger<ChatController> logger)
        {
            _chatService = chatService;
            _translator = translator;
            _logger = logger;
        }

        /// <summary>
        /// Send a message (authenticated). If the caller is a student, prefer the studentId from claims.
        /// Translates input/output when Language == "fr".
        /// </summary>
        [HttpPost("send")]
        [Authorize]
        public async Task<IActionResult> SendMessage([FromBody] ChatDto dto)
        {
            try
            {
                if (dto == null) return BadRequest(new { message = "Missing payload." });

                // Prefer authenticated student id if present
                var callerStudentIdClaim = User.FindFirst("studentId")?.Value
                                           ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(callerStudentIdClaim) && int.TryParse(callerStudentIdClaim, out var callerId))
                {
                    dto.StudentId = callerId;
                }

                var lang = string.IsNullOrEmpty(dto.Language) ? "en" : dto.Language;
                var userMessage = dto.Message ?? string.Empty;

                if (lang == "fr")
                {
                    userMessage = await _translator.TranslateAsync(userMessage, "en");
                }

                var botResponse = await _chatService.ProcessMessage(new ChatDto
                {
                    StudentId = dto.StudentId,
                    Message = userMessage,
                    Language = "en"
                });

                if (lang == "fr")
                {
                    botResponse = await _translator.TranslateAsync(botResponse, "fr");
                }

                return Ok(new { response = botResponse });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing SendMessage for student {StudentId}", dto?.StudentId);
                return StatusCode(500, new { message = "An error occurred while processing the message." });
            }
        }

        /// <summary>
        /// Get full chat history for a student. Admins may fetch any student's history; students may fetch their own.
        /// </summary>
        [HttpGet("history/{studentId}")]
        [Authorize]
        public async Task<IActionResult> GetHistory(int studentId)
        {
            try
            {
                var callerRole = User.FindFirst("role")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                var callerIdClaim = User.FindFirst("studentId")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                if (!string.Equals(callerRole, "Admin", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrEmpty(callerIdClaim) || !int.TryParse(callerIdClaim, out var callerId) || callerId != studentId)
                    {
                        return Forbid();
                    }
                }

                var chats = await _chatService.GetChatHistory(studentId);
                return Ok(chats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching chat history for student {StudentId}", studentId);
                return StatusCode(500, new { message = "An error occurred while fetching chat history." });
            }
        }

        /// <summary>
        /// Escalate a chat message into a ticket. Requires authentication.
        /// </summary>
        [HttpPost("escalate")]
        [Authorize]
        public async Task<IActionResult> Escalate([FromBody] ChatDto dto)
        {
            try
            {
                if (dto == null) return BadRequest(new { message = "Missing payload." });

                // Prefer authenticated student id if present
                var callerStudentIdClaim = User.FindFirst("studentId")?.Value
                                           ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(callerStudentIdClaim) && int.TryParse(callerStudentIdClaim, out var callerId))
                {
                    dto.StudentId = callerId;
                }

                var ticket = await _chatService.EscalateToTicket(dto);
                return Ok(ticket);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error escalating chat for student {StudentId}", dto?.StudentId);
                return StatusCode(500, new { message = "An error occurred while escalating the chat." });
            }
        }

        /// <summary>
        /// Return a small list of recent conversations for the sidebar.
        /// Requires authentication. Admins may request for any student; students only for their own id.
        /// Example: GET /api/chats/recent?studentId=123&take=5
        /// </summary>
        [HttpGet("recent")]
        [Authorize]
        public async Task<IActionResult> GetRecent([FromQuery] int studentId, [FromQuery] int take = 5)
        {
            try
            {
                if (studentId <= 0) return BadRequest(new { message = "Invalid studentId." });

                var callerRole = User.FindFirst("role")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                var callerIdClaim = User.FindFirst("studentId")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                if (!string.Equals(callerRole, "Admin", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrEmpty(callerIdClaim) || !int.TryParse(callerIdClaim, out var callerId) || callerId != studentId)
                    {
                        return Forbid();
                    }
                }

                // Use ChatService to get full history then select recent items (service preserves ordering)
                var allChats = await _chatService.GetChatHistory(studentId);
                var recent = allChats
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(Math.Max(1, Math.Min(20, take))) // clamp take between 1 and 20
                    .Select(c => new RecentConversationDto
                    {
                        Id = c.Id,
                        Preview = c.Message?.Length > 80 ? c.Message.Substring(0, 80) + "…" : (c.Message ?? string.Empty),
                        UpdatedAt = c.CreatedAt
                    })
                    .ToList();

                return Ok(recent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching recent conversations for student {StudentId}", studentId);
                return StatusCode(500, new { message = "An error occurred while fetching recent conversations." });
            }
        }

        // Compact DTO for recent conversations
        private class RecentConversationDto
        {
            public int Id { get; set; }
            public string Preview { get; set; } = string.Empty;
            public DateTime UpdatedAt { get; set; }
        }
    }
}
