using System.Security.Claims;
using System.Text.Json;
using BankingApi.Application.DTOs.Payments;
using BankingApi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankingApi.Api.Controllers
{
    [ApiController]
    [Route("api/v1/payments")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentsController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [Authorize(Roles = "Customer")]
        [HttpPost("initialize")]
        public async Task<IActionResult> InitializeDeposit([FromBody] InitializePaymentRequest request)
        {
            var userId = GetUserId();
            var result = await _paymentService.InitializeDepositAsync(request, userId);
            return Ok(new { success = true, data = result, message = "Payment initialized", timestamp = DateTimeOffset.UtcNow });
        }

        [AllowAnonymous]
        [HttpPost("webhook")]
        public async Task<IActionResult> HandleWebhook()
        {
            var rawBody = await _paymentService.GetRawBodyAsync(Request.Body);
            var signature = Request.Headers["x-paystack-signature"].FirstOrDefault() ?? string.Empty;

            if (!_paymentService.VerifyWebhookSignature(rawBody, signature))
                return Unauthorized(new { success = false, message = "Invalid signature" });

            PaystackWebhookEvent? webhookEvent;
            try
            {
                webhookEvent = JsonSerializer.Deserialize<PaystackWebhookEvent>(rawBody);
            }
            catch
            {
                return Ok(new { success = false, message = "Invalid payload" });
            }

            var ourReference = webhookEvent?.Data?.Reference;
            if (string.IsNullOrEmpty(ourReference))
                return Ok(new { success = false, message = "No reference" });

            var paystackReference = webhookEvent?.Data?.PaystackReference;
            var webhookAmount = webhookEvent?.Data != null ? webhookEvent.Data.Amount / 100m : 0m;

            switch (webhookEvent?.Event)
            {
                case "charge.success":
                    await _paymentService.HandleSuccessfulPaymentAsync(ourReference, paystackReference, webhookAmount);
                    break;

                case "transfer.success":
                    await _paymentService.HandleSuccessfulWithdrawalAsync(ourReference, paystackReference, webhookAmount);
                    break;

                default:
                    return Ok(new { success = true, message = "Event ignored" });
            }

            return Ok(new { success = true, message = "Payment processed" });
        }

        private Guid GetUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.Parse(claim!);
        }
    }

    public class PaystackWebhookEvent
    {
        public string? Event { get; set; }
        public PaystackWebhookData? Data { get; set; }
    }

    public class PaystackWebhookData
    {
        public string? Reference { get; set; }
        public string? PaystackReference { get; set; }
        public decimal Amount { get; set; }
        public PaystackWebhookMetadata? Metadata { get; set; }
    }

    public class PaystackWebhookMetadata
    {
        public string? AccountId { get; set; }
        public string? UserId { get; set; }
    }
}
