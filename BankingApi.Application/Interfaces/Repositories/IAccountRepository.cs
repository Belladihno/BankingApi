using BankingApi.Domain.Entities;
using BankingApi.Domain.Enums;

namespace BankingApi.Application.Interfaces.Repositories
{
    public interface IAccountRepository
    {
        Task<Account?> GetByIdAsync(Guid id);
        Task<Account?> GetByAccountNumberAsync(string accountNumber);
        Task<List<Account>> GetByOwnerIdAsync(Guid ownerId);
        Task<List<Account>> GetAllAsync(int page, int pageSize, AccountStatus? status, AccountType? accountType);
        Task AddAsync(Account account);
        Task UpdateAsync(Account account);
        Task<int> SaveChangesAsync();
    }
}
