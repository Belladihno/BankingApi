using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BankingApi.Application.DTOs.Payments;
using BankingApi.Application.Interfaces;
using BankingApi.Application.Interfaces.Repositories;
using BankingApi.Domain.Entities;
using BankingApi.Domain.Enums;
using BankingApi.Domain.Exceptions;
using BankingApi.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BankingApi.Infrastructure.Services
{
    public class PaystackService : IPaymentService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };
        private readonly HttpClient _httpClient;
        private readonly PaystackSettings _settings;
        private readonly ApplicationDbContext _context;
        private readonly IAccountRepository _accountRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly ILogger<PaystackService> _logger;

        public PaystackService(
            HttpClient httpClient,
            IOptions<PaystackSettings> settings,
            ApplicationDbContext context,
            IAccountRepository accountRepository,
            IPaymentRepository paymentRepository,
            ITransactionRepository transactionRepository,
            ILogger<PaystackService> logger)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _context = context;
            _accountRepository = accountRepository;
            _paymentRepository = paymentRepository;
            _transactionRepository = transactionRepository;
            _logger = logger;

            _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _settings.SecretKey);
        }

        public async Task<InitializePaymentResponse> InitializeDepositAsync(InitializePaymentRequest request, Guid userId)
        {
            var account = await _accountRepository.GetByIdAsync(request.AccountId);
            if (account == null)
                throw new DomainException("Account not found", "ACCOUNT_NOT_FOUND");

            if (account.OwnerId != userId)
                throw new DomainException("Account does not belong to you", "FORBIDDEN");

            if (request.Amount < 100)
                throw new DomainException("Minimum deposit is ₦100", "MIN_AMOUNT");

            var reference = $"PAY-{Guid.NewGuid():N}"[..20].ToUpper();

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                AccountId = request.AccountId,
                Amount = request.Amount,
                Reference = reference,
                Status = PaymentStatus.Pending,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await _paymentRepository.AddAsync(payment);
            await _paymentRepository.SaveChangesAsync();

            var amountInKobo = (int)(request.Amount * 100);

            var body = new
            {
                email = request.Email,
                amount = amountInKobo,
                reference,
                currency = "NGN",
                metadata = new
                {
                    accountId = request.AccountId.ToString(),
                    userId = userId.ToString()
                }
            };

            var json = JsonSerializer.Serialize(body);
            _logger.LogInformation("Calling Paystack /transaction/initialize with: {Json}", json);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage? response = null;
            try
            {
                response = await _httpClient.PostAsync("/transaction/initialize", content);
            }
            catch
            {
                payment.Status = PaymentStatus.Failed;
                await _paymentRepository.UpdateAsync(payment);
                await _paymentRepository.SaveChangesAsync();
                throw new DomainException("Payment initialization failed — Paystack unreachable", "PAYSTACK_ERROR");
            }

            var responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Paystack returned {StatusCode}: {Body}", response.StatusCode, responseJson);
                payment.Status = PaymentStatus.Failed;
                await _paymentRepository.UpdateAsync(payment);
                await _paymentRepository.SaveChangesAsync();
                throw new DomainException("Payment initialization failed", "PAYSTACK_ERROR");
            }

            var result = JsonSerializer.Deserialize<PaystackInitializeResponse>(responseJson, JsonOptions)
                ?? throw new DomainException("Invalid Paystack response", "PAYSTACK_ERROR");

            if (!result.Status)
            {
                _logger.LogError("Paystack returned status false: {Body}", responseJson);
                payment.Status = PaymentStatus.Failed;
                payment.PaystackReference = result.Data?.Reference;
                await _paymentRepository.UpdateAsync(payment);
                await _paymentRepository.SaveChangesAsync();
                throw new DomainException(result.Message ?? "Payment initialization failed", "PAYSTACK_ERROR");
            }

            payment.PaystackAccessCode = result.Data?.AccessCode;
            await _paymentRepository.UpdateAsync(payment);
            await _paymentRepository.SaveChangesAsync();

            return new InitializePaymentResponse
            {
                AuthorizationUrl = result.Data?.AuthorizationUrl ?? string.Empty,
                Reference = reference,
                AccessCode = result.Data?.AccessCode ?? string.Empty,
                Amount = request.Amount
            };
        }

        public async Task<InitializePaymentResponse> WithdrawToBankAsync(InitializePaymentRequest request, Guid userId)
        {
            var account = await _accountRepository.GetByIdAsync(request.AccountId);
            if (account == null)
                throw new DomainException("Account not found", "ACCOUNT_NOT_FOUND");

            if (account.OwnerId != userId)
                throw new DomainException("Account does not belong to you", "FORBIDDEN");

            if (account.Balance < request.Amount)
                throw new InsufficientFundsException("Insufficient balance for withdrawal");

            var reference = $"WTH-{Guid.NewGuid():N}"[..20].ToUpper();

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                AccountId = request.AccountId,
                Amount = request.Amount,
                Reference = reference,
                Status = PaymentStatus.Pending,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await _paymentRepository.AddAsync(payment);
            await _paymentRepository.SaveChangesAsync();

            var amountInKobo = (int)(request.Amount * 100);

            var body = new
            {
                source = "balance",
                amount = amountInKobo,
                reference,
                currency = "NGN",
                reason = $"Withdrawal from account {account.AccountNumber}"
            };

            var json = JsonSerializer.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage? response = null;
            try
            {
                response = await _httpClient.PostAsync("/transfer", content);
            }
            catch
            {
                payment.Status = PaymentStatus.Failed;
                await _paymentRepository.UpdateAsync(payment);
                await _paymentRepository.SaveChangesAsync();
                throw new DomainException("Withdrawal initiation failed — Paystack unreachable", "PAYSTACK_ERROR");
            }

            var responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                payment.Status = PaymentStatus.Failed;
                await _paymentRepository.UpdateAsync(payment);
                await _paymentRepository.SaveChangesAsync();
                throw new DomainException("Withdrawal initiation failed", "PAYSTACK_ERROR");
            }

            var result = JsonSerializer.Deserialize<PaystackTransferResponse>(responseJson, JsonOptions)
                ?? throw new DomainException("Invalid Paystack response", "PAYSTACK_ERROR");

            if (!result.Status)
            {
                payment.Status = PaymentStatus.Failed;
                payment.PaystackReference = result.Data?.Reference;
                await _paymentRepository.UpdateAsync(payment);
                await _paymentRepository.SaveChangesAsync();
                throw new DomainException(result.Message ?? "Withdrawal initiation failed", "PAYSTACK_ERROR");
            }

            payment.PaystackReference = result.Data?.Reference;
            await _paymentRepository.UpdateAsync(payment);
            await _paymentRepository.SaveChangesAsync();

            return new InitializePaymentResponse
            {
                AuthorizationUrl = string.Empty,
                Reference = reference,
                AccessCode = string.Empty,
                Amount = request.Amount
            };
        }

        public async Task HandleSuccessfulWithdrawalAsync(string reference, string? paystackRef, decimal amount)
        {
            var existingPayment = await _paymentRepository.GetByReferenceAsync(reference);
            if (existingPayment == null || existingPayment.Status == PaymentStatus.Success)
                return;

            if (existingPayment.Amount != amount)
                return;

            var account = await _accountRepository.GetByIdAsync(existingPayment.AccountId);
            if (account == null)
                return;

            if (account.Balance < existingPayment.Amount)
                return;

            account.Balance -= existingPayment.Amount;
            account.UpdatedAt = DateTimeOffset.UtcNow;

            existingPayment.Status = PaymentStatus.Success;
            existingPayment.CompletedAt = DateTimeOffset.UtcNow;
            existingPayment.PaystackReference = paystackRef;

            await using var dbTransaction = await _context.Database.BeginTransactionAsync();

            try
            {
                await _accountRepository.UpdateAsync(account);
                await _paymentRepository.UpdateAsync(existingPayment);
                await _paymentRepository.SaveChangesAsync();

                await dbTransaction.CommitAsync();
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }

        public async Task<string> GetRawBodyAsync(Stream body)
        {
            using var reader = new StreamReader(body, Encoding.UTF8);
            return await reader.ReadToEndAsync();
        }

        public bool VerifyWebhookSignature(string rawBody, string signature)
        {
            var keyBytes = Encoding.UTF8.GetBytes(_settings.SecretKey);
            var bodyBytes = Encoding.UTF8.GetBytes(rawBody);

            using var hmac = new HMACSHA512(keyBytes);
            var hash = hmac.ComputeHash(bodyBytes);
            var computedSignature = Convert.ToHexString(hash).ToLower();

            return computedSignature == signature.ToLower();
        }

        public async Task HandleSuccessfulPaymentAsync(string reference, string? paystackRef, decimal amount)
        {
            var existingPayment = await _paymentRepository.GetByReferenceAsync(reference);
            if (existingPayment == null || existingPayment.Status == PaymentStatus.Success)
                return;

            if (existingPayment.Amount != amount)
                return;

            var account = await _accountRepository.GetByIdAsync(existingPayment.AccountId);
            if (account == null)
                return;

            account.Balance += existingPayment.Amount;
            account.UpdatedAt = DateTimeOffset.UtcNow;

            existingPayment.Status = PaymentStatus.Success;
            existingPayment.CompletedAt = DateTimeOffset.UtcNow;
            existingPayment.PaystackReference = paystackRef;

            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                Reference = $"TXN-{Guid.NewGuid():N}"[..20].ToUpper(),
                Type = TransactionType.Deposit,
                Amount = existingPayment.Amount,
                DestinationAccountId = account.Id,
                Description = $"Paystack deposit — ref: {reference}",
                Status = TransactionStatus.Completed,
                PinVerified = false,
                InitiatedBy = account.OwnerId,
                CreatedAt = DateTimeOffset.UtcNow,
                CompletedAt = DateTimeOffset.UtcNow
            };

            var ledgerEntry = new TransactionLedgerEntry
            {
                Id = Guid.NewGuid(),
                TransactionId = transaction.Id,
                AccountId = account.Id,
                EntryType = LedgerEntryType.Credit,
                Amount = existingPayment.Amount,
                BalanceBefore = account.Balance - existingPayment.Amount,
                BalanceAfter = account.Balance,
                CreatedAt = DateTimeOffset.UtcNow
            };
            transaction.LedgerEntries.Add(ledgerEntry);

            await using var dbTransaction = await _context.Database.BeginTransactionAsync();

            try
            {
                await _transactionRepository.AddAsync(transaction);
                await _transactionRepository.AddLedgerEntriesAsync(transaction.LedgerEntries);
                await _paymentRepository.UpdateAsync(existingPayment);
                await _accountRepository.UpdateAsync(account);
                await _paymentRepository.SaveChangesAsync();

                await dbTransaction.CommitAsync();
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }

        private class PaystackInitializeResponse
        {
            public bool Status { get; set; }
            public string? Message { get; set; }
            public PaystackInitializeData? Data { get; set; }
        }

        private class PaystackInitializeData
        {
            [System.Text.Json.Serialization.JsonPropertyName("authorization_url")]
            public string? AuthorizationUrl { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("access_code")]
            public string? AccessCode { get; set; }

            public string? Reference { get; set; }
        }

        private class PaystackTransferResponse
        {
            public bool Status { get; set; }
            public string? Message { get; set; }
            public PaystackTransferData? Data { get; set; }
        }

        private class PaystackTransferData
        {
            public string? Reference { get; set; }
            public decimal Amount { get; set; }
            public string? Status { get; set; }
        }
    }
}
