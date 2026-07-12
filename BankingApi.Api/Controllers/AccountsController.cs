using System.Security.Claims;
using BankingApi.Application.DTOs.Accounts;
using BankingApi.Application.Interfaces;
using BankingApi.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankingApi.Api.Controllers
{
    [ApiController]
    [Route("api/v1/accounts")]
    public class AccountsController : ControllerBase
    {
        private readonly IAccountService _accountService;

        public AccountsController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        [Authorize(Roles = "Teller,Admin")]
        [HttpPost]
        public async Task<IActionResult> OpenAccount([FromBody] OpenAccountRequest request)
        {
            var initiatedBy = GetUserId();
            var result = await _accountService.OpenAccountAsync(request, initiatedBy);
            return CreatedAtAction(nameof(OpenAccount), new { success = true, data = result, message = "Account created successfully", timestamp = DateTimeOffset.UtcNow });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAllAccounts([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] AccountStatus? status = null, [FromQuery] AccountType? accountType = null)
        {
            var result = await _accountService.GetAllAccountsAsync(page, pageSize, status, accountType);
            return Ok(new { success = true, data = result, timestamp = DateTimeOffset.UtcNow });
        }

        [Authorize(Roles = "Customer,Admin,Teller")]
        [HttpGet("my-accounts")]
        public async Task<IActionResult> GetMyAccounts()
        {
            var userId = GetUserId();
            var result = await _accountService.GetAccountsByOwnerAsync(userId);
            return Ok(new { success = true, data = result, timestamp = DateTimeOffset.UtcNow });
        }

        [Authorize]
        [HttpGet("{accountId}")]
        public async Task<IActionResult> GetAccountById(Guid accountId)
        {
            var userId = GetUserId();
            var role = GetUserRole();
            var result = await _accountService.GetAccountByIdAsync(accountId, userId, role);
            return Ok(new { success = true, data = result, timestamp = DateTimeOffset.UtcNow });
        }

        [Authorize(Roles = "Teller,Admin")]
        [HttpGet("by-number/{accountNumber}")]
        public async Task<IActionResult> GetAccountByNumber(string accountNumber)
        {
            var result = await _accountService.GetAccountByNumberAsync(accountNumber);
            return Ok(new { success = true, data = result, timestamp = DateTimeOffset.UtcNow });
        }

        [Authorize(Roles = "Admin")]
        [HttpPatch("{accountId}/status")]
        public async Task<IActionResult> UpdateStatus(Guid accountId, [FromBody] UpdateAccountStatusRequest request)
        {
            var result = await _accountService.UpdateStatusAsync(accountId, request.Status, request.Reason);
            return Ok(new { success = true, data = result, message = "Account status updated", timestamp = DateTimeOffset.UtcNow });
        }

        [Authorize(Roles = "Admin")]
        [HttpPatch("{accountId}/daily-limit")]
        public async Task<IActionResult> UpdateDailyLimit(Guid accountId, [FromBody] UpdateDailyLimitRequest request)
        {
            var result = await _accountService.UpdateDailyLimitAsync(accountId, request.NewLimit);
            return Ok(new { success = true, data = result, message = "Daily limit updated", timestamp = DateTimeOffset.UtcNow });
        }

        private Guid GetUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.Parse(claim!);
        }

        private string GetUserRole()
        {
            return User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        }
    }

    public class UpdateDailyLimitRequest
    {
        public decimal NewLimit { get; set; }
    }
}
