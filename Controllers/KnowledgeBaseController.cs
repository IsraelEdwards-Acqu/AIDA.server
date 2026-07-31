using AIDA.Server.DTOs;
using AIDA.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using Microsoft.Extensions.Logging;

namespace AIDA.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableCors("AllowFrontend")]
    public class KnowledgeBaseController : ControllerBase
    {
        private readonly KnowledgeService _knowledgeService;
        private readonly ILogger<KnowledgeBaseController> _logger;

        public KnowledgeBaseController(KnowledgeService knowledgeService, ILogger<KnowledgeBaseController> logger)
        {
            _knowledgeService = knowledgeService;
            _logger = logger;
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string q)
        {
            try
            {
                var result = await _knowledgeService.Search(q);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching knowledge base for query: {Query}", q);
                return StatusCode(500, new { message = "An error occurred while searching the knowledge base." });
            }
        }

        [HttpPost("add")]
        public async Task<IActionResult> Add([FromBody] KnowledgeDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Request body is required." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var entry = await _knowledgeService.AddEntry(dto);
                return Ok(entry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding knowledge entry");
                return StatusCode(500, new { message = "An error occurred while adding the knowledge entry." });
            }
        }

        [HttpPut("promote/{chatId}")]
        public async Task<IActionResult> Promote(int chatId)
        {
            if (chatId <= 0)
                return BadRequest(new { message = "Invalid chatId." });

            try
            {
                var entry = await _knowledgeService.PromoteChat(chatId);
                return Ok(entry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error promoting chat {ChatId} to knowledge entry", chatId);
                return StatusCode(500, new { message = "An error occurred while promoting the chat to the knowledge base." });
            }
        }
    }
}
