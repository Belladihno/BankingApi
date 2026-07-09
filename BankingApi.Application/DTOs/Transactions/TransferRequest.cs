namespace BankingApi.Application.DTOs.Transactions
{
    public class TransferRequest
    {
        public Guid SourceAccountId { get; set; }
        public Guid DestinationAccountId { get; set; }
        public decimal Amount { get; set; }
        public string TransactionPin { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
