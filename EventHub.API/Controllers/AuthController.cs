using EventHub.Application.Dtos.AuthDtos;
using EventHub.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

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
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        // Confirms the email with the 6-digit code from the email, then logs the user in
        [HttpPost("confirm-code")]
        [EnableRateLimiting("fixed")]
        public async Task<IActionResult> ConfirmCode([FromBody] ConfirmCodeDto dto)
        {
            var result = await _authService.ConfirmCodeAsync(dto.Email, dto.Code);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPost("resend-code")]
        [EnableRateLimiting("fixed")]
        public async Task<IActionResult> ResendCode([FromBody] ResendCodeDto dto)
        {
            var result = await _authService.ResendConfirmationCodeAsync(dto.Email);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var result = await _authService.LoginUserAsync(dto);
            if (!result.IsSuccess) {return Unauthorized(result); }
            return Ok(result);
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto dto)
        {
            
            var result = await _authService.RefreshTokenAsync(dto);
            return result.IsSuccess ? Ok(result) : Unauthorized(result);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody]LogOutDto dto)
        {
            var result = await _authService.LogoutAsync(dto.RefreshToken);
            return result.IsSuccess ? NoContent() : BadRequest(result);
        }


        [HttpPost("register-organization")]
        public async Task<IActionResult> RegisterOrganization([FromForm] OrganizationRegisterDto dto)
        {
            var result = await _authService.RegisterOrganizationAsync(dto);
            return result.IsSuccess? Ok(result) : BadRequest(result);
        }

        [HttpPost("forget-password")]
        [EnableRateLimiting("fixed")]
        public async Task<IActionResult> ForgetPassword([FromBody] ForgetPasswordDto dto)
        { 
            var result = await _authService.ForgetPasswordAsync(dto);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPost("reset-password")]
        [EnableRateLimiting("fixed")]
        public async Task<IActionResult> ResetPassword([FromBody]ResetPasswordDto dto)
        { 
            var result = await _authService.ResetPasswordAsync(dto);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        // GET: api/Auth/login-google
        [HttpGet("login-google")]
        public async Task <IActionResult> LoginGoogle()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(GoogleResponse))
            };

            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet("google-response")]
        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

            if (!result.Succeeded)
                return BadRequest("Google authentication failed.");

            var response = await _authService.GoogleResponseAsync(result);

            if (!response.IsAuthenticated)
                return BadRequest(response);

            return Ok(response);
        }
    }
}
