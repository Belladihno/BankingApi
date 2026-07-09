using BankingApi.Domain.Enums;

namespace BankingApi.Application.DTOs.Transactions
{
    public class LedgerEntryResponse
    {
        public Guid Id { get; set; }
        public Guid AccountId { get; set; }
        public LedgerEntryType EntryType { get; set; }
        public decimal Amount { get; set; }
        public decimal BalanceBefore { get; set; }
        public decimal BalanceAfter { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
