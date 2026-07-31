using AIDA.Server.DTOs;
using AIDA.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using Microsoft.Extensions.Logging;

namespace AIDA.Server.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [EnableCors("AllowFrontend")] // ensure this controller honors the AllowFrontend CORS policy
    public class AdminAuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly ILogger<AdminAuthController> _logger;

        public AdminAuthController(AuthService authService, ILogger<AdminAuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Admin login endpoint
        /// POST /api/admin/login
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        [Consumes("application/json")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Login([FromBody] AdminLoginDto dto)
        {
            // Defensive: ensure payload present
            if (dto == null)
            {
                _logger.LogWarning("Admin login called with empty payload. Origin: {Origin}", Request.Headers["Origin"].ToString());
                return BadRequest(new { message = "Missing login payload." });
            }

            try
            {
                // Log origin and username (do not log passwords)
                var origin = Request.Headers["Origin"].ToString();
                _logger.LogInformation("Admin login attempt for user {username} from origin {origin}", dto.Username, origin);

                var loginResult = await _authService.AdminLogin(dto);

                if (loginResult == null || string.IsNullOrEmpty(loginResult.Token))
                {
                    _logger.LogWarning("Invalid admin credentials for user {username} from origin {origin}", dto.Username, origin);
                    return Unauthorized(new { message = "Invalid admin credentials" });
                }

                // Return token and role
                return Ok(new
                {
                    token = loginResult.Token,
                    role = "Admin"
                });
            }
            catch (Exception ex)
            {
                // Log the exception so server logs show the root cause (helps diagnose 500s)
                _logger.LogError(ex, "Error while attempting admin login for user {username}", dto?.Username);

                // Return a generic 500 response (do not leak sensitive details)
                return StatusCode(500, new { message = "An error occurred while processing the login request." });
            }
        }
    }
}
