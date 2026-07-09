using BankingApi.Domain.Enums;

namespace BankingApi.Application.DTOs.Accounts
{
    public class OpenAccountRequest
    {
        public Guid OwnerId { get; set; }
        public AccountType AccountType { get; set; }
        public decimal OpeningDeposit { get; set; }
        public string? Description { get; set; }
    }
}
