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
    [Route("api/admin/knowledge")]
    [EnableCors("AllowFrontend")] // ensure CORS policy is applied for frontend requests
    public class AdminKnowledgeController : ControllerBase
    {
        private readonly KnowledgeService _knowledgeService;
        private readonly ILogger<AdminKnowledgeController> _logger;

        public AdminKnowledgeController(KnowledgeService knowledgeService, ILogger<AdminKnowledgeController> logger)
        {
            _knowledgeService = knowledgeService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var entries = await _knowledgeService.GetAllEntries();
                return Ok(entries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching knowledge entries");
                return StatusCode(500, new { message = "An error occurred while fetching knowledge entries." });
            }
        }

        [HttpPost]
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

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] KnowledgeDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Request body is required." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var success = await _knowledgeService.UpdateEntry(id, dto);
                return success ? Ok(new { message = "Entry updated" }) : NotFound(new { message = "Entry not found." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating knowledge entry with id {Id}", id);
                return StatusCode(500, new { message = "An error occurred while updating the knowledge entry." });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var success = await _knowledgeService.DeleteEntry(id);
                return success ? Ok(new { message = "Entry deleted" }) : NotFound(new { message = "Entry not found." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting knowledge entry with id {Id}", id);
                return StatusCode(500, new { message = "An error occurred while deleting the knowledge entry." });
            }
        }
    }
}
