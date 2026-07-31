using AIDA.Server.DTOs;
using AIDA.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using Microsoft.Extensions.Logging;

namespace AIDA.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableCors("AllowFrontend")] // ensure frontend origin is allowed
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(AuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("signup")]
        [Produces("application/json")]
        public async Task<IActionResult> Signup([FromBody] StudentSignupDto dto)
        {
            try
            {
                var result = await _authService.Signup(dto);
                return Ok(new { message = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during signup for student {StudentId}", dto?.StudentId);
                return StatusCode(500, new { message = "An error occurred while processing the signup request." });
            }
        }

        [HttpPost("login")]
        [Produces("application/json")]
        public async Task<IActionResult> Login([FromBody] StudentLoginDto dto)
        {
            try
            {
                var loginResult = await _authService.Login(dto);

                if (loginResult == null || string.IsNullOrEmpty(loginResult.Token))
                    return Unauthorized(new { message = "Invalid credentials" });

                return Ok(new
                {
                    token = loginResult.Token,
                    studentId = loginResult.StudentId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for student {StudentId}", dto?.StudentId);
                return StatusCode(500, new { message = "An error occurred while processing the login request." });
            }
        }
    }
}
