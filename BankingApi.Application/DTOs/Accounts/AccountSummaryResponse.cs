using BankingApi.Domain.Enums;

namespace BankingApi.Application.DTOs.Accounts
{
    public class AccountSummaryResponse
    {
        public Guid Id { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public AccountType AccountType { get; set; }
        public decimal Balance { get; set; }
        public AccountStatus Status { get; set; }
    }
}
