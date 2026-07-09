using BankingApi.Application.Interfaces.Repositories;
using BankingApi.Domain.Entities;
using BankingApi.Domain.Enums;
using BankingApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BankingApi.Infrastructure.Repositories
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly ApplicationDbContext _context;

        public TransactionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Transaction?> GetByIdAsync(Guid id)
        {
            return await _context.Transactions
                .Include(t => t.LedgerEntries)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<Transaction?> GetByReferenceAsync(string reference)
        {
            return await _context.Transactions
                .FirstOrDefaultAsync(t => t.Reference == reference);
        }

        public async Task<List<Transaction>> GetByAccountIdAsync(Guid accountId, int page, int pageSize, TransactionType? type, DateTimeOffset? startDate, DateTimeOffset? endDate, TransactionStatus? status)
        {
            var query = _context.Transactions
                .Where(t => t.SourceAccountId == accountId || t.DestinationAccountId == accountId);

            if (type.HasValue)
                query = query.Where(t => t.Type == type.Value);

            if (startDate.HasValue)
                query = query.Where(t => t.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(t => t.CreatedAt <= endDate.Value);

            if (status.HasValue)
                query = query.Where(t => t.Status == status.Value);

            return await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task AddAsync(Transaction transaction)
        {
            await _context.Transactions.AddAsync(transaction);
        }

        public async Task AddLedgerEntriesAsync(IEnumerable<TransactionLedgerEntry> entries)
        {
            await _context.TransactionLedgerEntries.AddRangeAsync(entries);
        }

        public async Task<Transaction> ExecuteTransferAsync(Account sourceAccount, Account destinationAccount, Transaction transaction, List<TransactionLedgerEntry> ledgerEntries)
        {
            using var dbTransaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _context.Accounts.Update(sourceAccount);
                _context.Accounts.Update(destinationAccount);
                await _context.Transactions.AddAsync(transaction);
                await _context.TransactionLedgerEntries.AddRangeAsync(ledgerEntries);
                await _context.SaveChangesAsync();

                await dbTransaction.CommitAsync();
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                throw;
            }

            return transaction;
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
