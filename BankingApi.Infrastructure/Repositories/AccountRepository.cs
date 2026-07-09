using BankingApi.Application.Interfaces.Repositories;
using BankingApi.Domain.Entities;
using BankingApi.Domain.Enums;
using BankingApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BankingApi.Infrastructure.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly ApplicationDbContext _context;

        public AccountRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Account?> GetByIdAsync(Guid id)
        {
            return await _context.Accounts
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<Account?> GetByAccountNumberAsync(string accountNumber)
        {
            return await _context.Accounts
                .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);
        }

        public async Task<List<Account>> GetByOwnerIdAsync(Guid ownerId)
        {
            return await _context.Accounts
                .Where(a => a.OwnerId == ownerId && !a.IsDeleted)
                .ToListAsync();
        }

        public async Task<List<Account>> GetAllAsync(int page, int pageSize, AccountStatus? status, AccountType? accountType)
        {
            var query = _context.Accounts
                .Where(a => !a.IsDeleted);

            if (status.HasValue)
                query = query.Where(a => a.Status == status.Value);

            if (accountType.HasValue)
                query = query.Where(a => a.AccountType == accountType.Value);

            return await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task AddAsync(Account account)
        {
            await _context.Accounts.AddAsync(account);
        }

        public Task UpdateAsync(Account account)
        {
            _context.Accounts.Update(account);
            return Task.CompletedTask;
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
