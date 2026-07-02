using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class TranslateController : ControllerBase
{
    private readonly TranslationService _translationService;

    public TranslateController(TranslationService translationService)
    {
        _translationService = translationService;
    }

    [HttpPost]
    public async Task<IActionResult> Translate([FromBody] TranslateRequest request)
    {
        var translatedText = await _translationService.TranslateAsync(request.Text, request.TargetLang);
        return Ok(translatedText);
    }
}

public class TranslateRequest
{
    public string Text { get; set; }
    public string TargetLang { get; set; }
}
