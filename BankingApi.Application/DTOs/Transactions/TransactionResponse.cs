using BankingApi.Domain.Enums;

namespace BankingApi.Application.DTOs.Transactions
{
    public class TransactionResponse
    {
        public Guid Id { get; set; }
        public string Reference { get; set; } = string.Empty;
        public TransactionType Type { get; set; }
        public decimal Amount { get; set; }
        public Guid? SourceAccountId { get; set; }
        public Guid? DestinationAccountId { get; set; }
        public string Description { get; set; } = string.Empty;
        public TransactionStatus Status { get; set; }
        public bool PinVerified { get; set; }
        public string InitiatedByName { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }
        public List<LedgerEntryResponse> LedgerEntries { get; set; } = new();
    }
}
