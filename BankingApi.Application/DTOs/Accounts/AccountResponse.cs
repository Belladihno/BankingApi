using BankingApi.Domain.Enums;

namespace BankingApi.Application.DTOs.Accounts
{
    public class AccountResponse
    {
        public Guid Id { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public AccountType AccountType { get; set; }
        public decimal Balance { get; set; }
        public AccountStatus Status { get; set; }
        public decimal DailyWithdrawalLimit { get; set; }
        public decimal TodayWithdrawnAmount { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
    }
}
