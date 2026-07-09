namespace BankingApi.Application.DTOs.Transactions
{
    public class DepositRequest
    {
        public Guid AccountId { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? Reference { get; set; }
    }
}
