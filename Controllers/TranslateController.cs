using AIDA.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using Microsoft.Extensions.Logging;

namespace AIDA.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableCors("AllowFrontend")]
    public class TranslateController : ControllerBase
    {
        private readonly TranslationService _translationService;
        private readonly ILogger<TranslateController> _logger;

        public TranslateController(TranslationService translationService, ILogger<TranslateController> logger)
        {
            _translationService = translationService;
            _logger = logger;
        }

        [HttpPost]
        [Produces("application/json")]
        public async Task<IActionResult> Translate([FromBody] TranslateRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Request body is required." });

            if (string.IsNullOrWhiteSpace(request.Text))
                return BadRequest(new { message = "Text is required." });

            if (string.IsNullOrWhiteSpace(request.TargetLang))
                request.TargetLang = "en";

            try
            {
                var translatedText = await _translationService.TranslateAsync(request.Text, request.TargetLang);
                return Ok(new { text = translatedText });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error translating text to {TargetLang}", request.TargetLang);
                return StatusCode(500, new { message = "An error occurred while translating the text." });
            }
        }
    }

    public class TranslateRequest
    {
        public string Text { get; set; } = string.Empty;
        public string TargetLang { get; set; } = "en";
    }
}
