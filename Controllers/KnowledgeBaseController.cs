using AIDA.Server.DTOs;
using AIDA.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace AIDA.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class KnowledgeBaseController : ControllerBase
    {
        private readonly KnowledgeService _knowledgeService;

        public KnowledgeBaseController(KnowledgeService knowledgeService)
        {
            _knowledgeService = knowledgeService;
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string q)
        {
            var result = await _knowledgeService.Search(q);
            return Ok(result);
        }

        [HttpPost("add")]
        public async Task<IActionResult> Add(KnowledgeDto dto)
        {
            var entry = await _knowledgeService.AddEntry(dto);
            return Ok(entry);
        }

        [HttpPut("promote/{chatId}")]
        public async Task<IActionResult> Promote(int chatId)
        {
            var entry = await _knowledgeService.PromoteChat(chatId);
            return Ok(entry);
        }
    }

}
