using BankingApi.Application.DTOs.Accounts;
using BankingApi.Domain.Enums;

namespace BankingApi.Application.Interfaces
{
    public interface IAccountService
    {
        Task<AccountResponse> OpenAccountAsync(OpenAccountRequest request, Guid initiatedBy);
        Task<AccountResponse> GetAccountByIdAsync(Guid accountId, Guid userId, string role);
        Task<AccountResponse> GetAccountByNumberAsync(string accountNumber);
        Task<List<AccountSummaryResponse>> GetAccountsByOwnerAsync(Guid ownerId);
        Task<List<AccountResponse>> GetAllAccountsAsync(int page, int pageSize, AccountStatus? status, AccountType? accountType);
        Task<AccountResponse> UpdateStatusAsync(Guid accountId, AccountStatus newStatus, string reason);
        Task<AccountResponse> UpdateDailyLimitAsync(Guid accountId, decimal newLimit);
    }
}
