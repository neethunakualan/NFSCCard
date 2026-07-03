using Microsoft.AspNetCore.Mvc;
using NFSCCard.Services;
using NFSCCard.DTOs.Auth;

namespace NFSCCard.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _auth;
        public AuthController(IAuthService auth) => _auth = auth;

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            await _auth.RegisterAsync(dto);
            return Ok(new { message = "Registered" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                var res = await _auth.LoginAsync(dto);
                return Ok(res);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { message = "Invalid credentials" });
            }
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> Refresh([FromBody] string refreshToken)
        {
            var res = await _auth.RefreshTokenAsync(refreshToken);
            if (res == null) return Unauthorized();
            return Ok(res);
        }
    }
}
