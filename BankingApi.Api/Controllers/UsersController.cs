using BankingApi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankingApi.Api.Controllers
{
    [ApiController]
    [Route("api/v1/users")]
    [Authorize(Roles = "Admin")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] bool? isActive = null)
        {
            var result = await _userService.GetAllUsersAsync(page, pageSize, isActive);
            return Ok(new { success = true, data = result, timestamp = DateTimeOffset.UtcNow });
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUserById(Guid userId)
        {
            var result = await _userService.GetUserByIdAsync(userId);
            return Ok(new { success = true, data = result, timestamp = DateTimeOffset.UtcNow });
        }

        [HttpPatch("{userId}/deactivate")]
        public async Task<IActionResult> DeactivateUser(Guid userId)
        {
            await _userService.DeactivateUserAsync(userId);
            return Ok(new { success = true, message = "User deactivated", timestamp = DateTimeOffset.UtcNow });
        }

        [HttpPatch("{userId}/activate")]
        public async Task<IActionResult> ActivateUser(Guid userId)
        {
            await _userService.ActivateUserAsync(userId);
            return Ok(new { success = true, message = "User activated", timestamp = DateTimeOffset.UtcNow });
        }

        [HttpPost("{userId}/reset-pin")]
        public async Task<IActionResult> ResetPin(Guid userId)
        {
            await _userService.ResetPinAsync(userId);
            return Ok(new { success = true, message = "PIN reset. Customer must set up a new PIN.", timestamp = DateTimeOffset.UtcNow });
        }
    }
}
