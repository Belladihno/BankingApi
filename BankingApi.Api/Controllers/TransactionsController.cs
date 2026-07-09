using System.Security.Claims;
using BankingApi.Application.DTOs.Transactions;
using BankingApi.Application.Interfaces;
using BankingApi.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankingApi.Api.Controllers
{
    [ApiController]
    [Route("api/v1/transactions")]
    public class TransactionsController : ControllerBase
    {
        private readonly ITransactionService _transactionService;

        public TransactionsController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        [Authorize(Roles = "Teller,Admin")]
        [HttpPost("deposit")]
        public async Task<IActionResult> Deposit([FromBody] DepositRequest request)
        {
            var initiatedBy = GetUserId();
            var result = await _transactionService.DepositAsync(request, initiatedBy);
            return CreatedAtAction(nameof(Deposit), new { success = true, data = result, message = "Deposit successful", timestamp = DateTimeOffset.UtcNow });
        }

        [Authorize(Roles = "Customer")]
        [HttpPost("withdraw")]
        public async Task<IActionResult> Withdraw([FromBody] WithdrawRequest request)
        {
            var initiatedBy = GetUserId();
            var result = await _transactionService.WithdrawAsync(request, initiatedBy);
            return CreatedAtAction(nameof(Withdraw), new { success = true, data = result, message = "Withdrawal successful", timestamp = DateTimeOffset.UtcNow });
        }

        [Authorize(Roles = "Customer")]
        [HttpPost("transfer")]
        public async Task<IActionResult> Transfer([FromBody] TransferRequest request)
        {
            var initiatedBy = GetUserId();
            var result = await _transactionService.TransferAsync(request, initiatedBy);
            return CreatedAtAction(nameof(Transfer), new { success = true, data = result, message = "Transfer successful", timestamp = DateTimeOffset.UtcNow });
        }

        [Authorize]
        [HttpGet("{transactionId}")]
        public async Task<IActionResult> GetTransaction(Guid transactionId)
        {
            var userId = GetUserId();
            var role = GetUserRole();
            var result = await _transactionService.GetTransactionAsync(transactionId, userId, role);
            return Ok(new { success = true, data = result, timestamp = DateTimeOffset.UtcNow });
        }

        [Authorize]
        [HttpGet("account/{accountId}")]
        public async Task<IActionResult> GetAccountHistory(Guid accountId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] TransactionType? type = null, [FromQuery] DateTimeOffset? startDate = null, [FromQuery] DateTimeOffset? endDate = null, [FromQuery] TransactionStatus? status = null)
        {
            var userId = GetUserId();
            var role = GetUserRole();
            var result = await _transactionService.GetAccountHistoryAsync(accountId, userId, role, page, pageSize, type, startDate, endDate, status);
            return Ok(new { success = true, data = result, timestamp = DateTimeOffset.UtcNow });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("{transactionId}/reverse")]
        public async Task<IActionResult> ReverseTransaction(Guid transactionId, [FromBody] ReverseTransactionRequest request)
        {
            var initiatedBy = GetUserId();
            var result = await _transactionService.ReverseTransactionAsync(transactionId, request.Reason, initiatedBy);
            return Ok(new { success = true, data = result, message = "Transaction reversed", timestamp = DateTimeOffset.UtcNow });
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

    public class ReverseTransactionRequest
    {
        public string Reason { get; set; } = string.Empty;
    }
}
