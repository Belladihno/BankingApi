using BankingApi.Domain.Enums;

namespace BankingApi.Domain.Entities
{
    public class TransactionLedgerEntry
    {
        public Guid Id { get; set; }
        public Guid TransactionId { get; set; }
        public Guid AccountId { get; set; }
        public LedgerEntryType EntryType { get; set; }
        public decimal Amount { get; set; }
        public decimal BalanceBefore { get; set; }
        public decimal BalanceAfter { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public Transaction Transaction { get; set; } = null!;
        public Account Account { get; set; } = null!;
    }
}
