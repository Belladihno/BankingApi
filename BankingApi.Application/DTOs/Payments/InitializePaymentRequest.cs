namespace BankingApi.Application.DTOs.Payments
{
    public class InitializePaymentRequest
    {
        public Guid AccountId { get; set; }
        public decimal Amount { get; set; }
        public string Email { get; set; } = string.Empty;
    }
}
