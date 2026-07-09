namespace BankingApi.Application.DTOs.Transactions
{
    public class WithdrawRequest
    {
        public Guid AccountId { get; set; }
        public decimal Amount { get; set; }
        public string TransactionPin { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
