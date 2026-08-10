using EventHub.Application.DTOs.AuthDTOs;
using EventHub.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // Registers a new user 
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromForm] UserRegisterDto dto)
        {
            var result = await _authService.RegisterUserAsync(dto);
            return result.IsAuthenticated ? Ok(result) : BadRequest(result);
        }

        // Confirms the email with the 6-digit code from the email, then logs the user in
        [HttpPost("confirm-code")]
        public async Task<IActionResult> ConfirmCode([FromBody] ConfirmCodeDto dto)
        {
            var result = await _authService.ConfirmCodeAsync(dto.Email, dto.Code);
            return result.IsAuthenticated ? Ok(result) : BadRequest(result);
        }

        [HttpPost("register-organization")]
        public IActionResult RegisterOrganization([FromBody] OrganizationRegisterDto dto)
        {
            // TODO: implement organization registration
            return Ok();
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDTO dto)
        {
            // TODO: implement login
            return Ok();
        }

        [HttpPost("refresh-token")]
        public IActionResult RefreshToken()
        {
            // TODO: implement token refresh
            return Ok();
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            // TODO: implement logout
            return Ok();
        }

        [HttpGet("me")]
        public IActionResult GetProfile()
        {
            // TODO: implement getting user profile
            return Ok();
        }
    }
}
