using AIDA.Server.DTOs;
using AIDA.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/admin/knowledge")]
public class AdminKnowledgeController : ControllerBase
{
    private readonly KnowledgeService _knowledgeService;

    public AdminKnowledgeController(KnowledgeService knowledgeService)
    {
        _knowledgeService = knowledgeService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var entries = await _knowledgeService.GetAllEntries();
        return Ok(entries);
    }

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] KnowledgeDto dto)
    {
        var entry = await _knowledgeService.AddEntry(dto);
        return Ok(entry);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] KnowledgeDto dto)
    {
        var success = await _knowledgeService.UpdateEntry(id, dto);
        return success ? Ok(new { message = "Entry updated" }) : NotFound();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await _knowledgeService.DeleteEntry(id);
        return success ? Ok(new { message = "Entry deleted" }) : NotFound();
    }
}
