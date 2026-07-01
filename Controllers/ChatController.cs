using AIDA.Server.DTOs;
using AIDA.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace AIDA.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly ChatService _chatService;

        public ChatController(ChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage(ChatDto dto)
        {
            var response = await _chatService.ProcessMessage(dto);
            return Ok(response);
        }

        [HttpGet("history/{studentId}")]
        public async Task<IActionResult> GetHistory(int studentId)
        {
            var chats = await _chatService.GetChatHistory(studentId);
            return Ok(chats);
        }

        [HttpPost("escalate")]
        public async Task<IActionResult> Escalate(ChatDto dto)
        {
            var ticket = await _chatService.EscalateToTicket(dto);
            return Ok(ticket);
        }
    }

}
