using BankingApi.Domain.Enums;

namespace BankingApi.Domain.Entities
{
    public class Account
    {
        public Guid Id { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public AccountType AccountType { get; set; }
        public decimal Balance { get; set; }
        public AccountStatus Status { get; set; } = AccountStatus.Active;
        public decimal DailyWithdrawalLimit { get; set; }
        public decimal TodayWithdrawnAmount { get; set; }
        public Guid OwnerId { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
        public bool IsDeleted { get; set; }

        public ApplicationUser Owner { get; set; } = null!;
    }
}
