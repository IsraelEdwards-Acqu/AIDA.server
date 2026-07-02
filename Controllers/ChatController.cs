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
        private readonly TranslationService _translator;

        public ChatController(ChatService chatService, TranslationService translator)
        {
            _chatService = chatService;
            _translator = translator;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage(ChatDto dto)
        {
            var lang = string.IsNullOrEmpty(dto.Language) ? "en" : dto.Language;
            var userMessage = dto.Message;

            // If user selected French, translate input to English
            if (lang == "fr")
            {
                userMessage = await _translator.TranslateAsync(userMessage, "en");
            }

            // Generate bot response in English
            var botResponse = await _chatService.ProcessMessage(new ChatDto
            {
                StudentId = dto.StudentId,
                Message = userMessage,
                Language = "en"
            });

            // If user selected French, translate output back to French
            if (lang == "fr")
            {
                botResponse = await _translator.TranslateAsync(botResponse, "fr");
            }

            return Ok(new { response = botResponse });
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
