using BankingApi.Domain.Enums;

namespace BankingApi.Application.DTOs.Accounts
{
    public class UpdateAccountStatusRequest
    {
        public AccountStatus Status { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
