using AIDA.Server.DTOs;
using AIDA.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIDA.Server.Controllers
{
    [ApiController]
    [Route("api/admin")]
    public class AdminAuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AdminAuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        [AllowAnonymous] // ✅ allow login without token
        public async Task<IActionResult> Login(AdminLoginDto dto)
        {
            var loginResult = await _authService.AdminLogin(dto);

            if (loginResult == null || string.IsNullOrEmpty(loginResult.Token))
                return Unauthorized(new { message = "Invalid admin credentials" });

            // ✅ return token with role claim included
            return Ok(new
            {
                token = loginResult.Token,
                role = "Admin"
            });
        }
    }
}
