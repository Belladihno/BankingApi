using BankingApi.Domain.Enums;

namespace BankingApi.Domain.Entities
{
    public class Transaction
    {
        public Guid Id { get; set; }
        public string Reference { get; set; } = string.Empty;
        public TransactionType Type { get; set; }
        public decimal Amount { get; set; }
        public Guid? SourceAccountId { get; set; }
        public Guid? DestinationAccountId { get; set; }
        public string Description { get; set; } = string.Empty;
        public TransactionStatus Status { get; set; } = TransactionStatus.Pending;
        public bool PinVerified { get; set; }
        public Guid InitiatedBy { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? CompletedAt { get; set; }

        public Account? SourceAccount { get; set; }
        public Account? DestinationAccount { get; set; }
        public ApplicationUser Initiator { get; set; } = null!;
        public ICollection<TransactionLedgerEntry> LedgerEntries { get; set; } = new List<TransactionLedgerEntry>();
    }
}
