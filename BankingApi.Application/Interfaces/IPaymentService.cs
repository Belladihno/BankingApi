using BankingApi.Application.DTOs.Payments;

namespace BankingApi.Application.Interfaces
{
    public interface IPaymentService
    {
        Task<InitializePaymentResponse> InitializeDepositAsync(InitializePaymentRequest request, Guid userId);
        Task<InitializePaymentResponse> WithdrawToBankAsync(InitializePaymentRequest request, Guid userId);
        Task<string> GetRawBodyAsync(Stream body);
        bool VerifyWebhookSignature(string rawBody, string signature);
        Task HandleSuccessfulPaymentAsync(string reference, string? paystackReference, decimal amount);
        Task HandleSuccessfulWithdrawalAsync(string reference, string? paystackReference, decimal amount);
    }
}
