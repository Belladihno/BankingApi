using System.Security.Claims;
using BankingApi.Application.DTOs.Auth;
using BankingApi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankingApi.Api.Controllers
{
    [ApiController]
    [Route("api/v1/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterCustomerRequest request)
        {
            var result = await _authService.RegisterAsync(request);
            return CreatedAtAction(nameof(Register), new { success = true, data = result, message = "Registration successful", timestamp = DateTimeOffset.UtcNow });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _authService.LoginAsync(request);
            return Ok(new { success = true, data = result, message = "Login successful", timestamp = DateTimeOffset.UtcNow });
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var result = await _authService.RefreshTokenAsync(request.RefreshToken);
            return Ok(new { success = true, data = result, timestamp = DateTimeOffset.UtcNow });
        }

        [Authorize]
        [HttpPost("setup-pin")]
        public async Task<IActionResult> SetupPin([FromBody] SetupPinRequest request)
        {
            var userId = GetUserId();
            await _authService.SetupPinAsync(userId, request.Pin);
            return Ok(new { success = true, message = "PIN set successfully", timestamp = DateTimeOffset.UtcNow });
        }

        [Authorize]
        [HttpPost("change-pin")]
        public async Task<IActionResult> ChangePin([FromBody] ChangePinRequest request)
        {
            var userId = GetUserId();
            await _authService.ChangePinAsync(userId, request.CurrentPin, request.NewPin);
            return Ok(new { success = true, message = "PIN changed successfully", timestamp = DateTimeOffset.UtcNow });
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
        {
            var userId = GetUserId();
            await _authService.LogoutAsync(userId, request.RefreshToken);
            return NoContent();
        }

        private Guid GetUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.Parse(claim!);
        }
    }

    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }
}
