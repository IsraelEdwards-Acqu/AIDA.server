using AIDA.Server.DTOs;
using AIDA.Server.Helpers;
using AIDA.Server.Models;
using AIDA.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace AIDA.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("signup")]
        public async Task<IActionResult> Signup(StudentSignupDto dto)
        {
            var result = await _authService.Signup(dto);
            return Ok(new { message = result });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(StudentLoginDto dto)
        {
            var token = await _authService.Login(dto);
            if (token == null)
                return Unauthorized(new { message = "Invalid credentials" });

            return Ok(new { token });
        }
    }
}
