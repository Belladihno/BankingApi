using BankingApi.Domain.Entities;
using BankingApi.Domain.Enums;

namespace BankingApi.Application.Interfaces.Repositories
{
    public interface ITransactionRepository
    {
        Task<Transaction?> GetByIdAsync(Guid id);
        Task<Transaction?> GetByReferenceAsync(string reference);
        Task<List<Transaction>> GetByAccountIdAsync(Guid accountId, int page, int pageSize, TransactionType? type, DateTimeOffset? startDate, DateTimeOffset? endDate, TransactionStatus? status);
        Task AddAsync(Transaction transaction);
        Task AddLedgerEntriesAsync(IEnumerable<TransactionLedgerEntry> entries);
        Task<Transaction> ExecuteTransferAsync(Account sourceAccount, Account destinationAccount, Transaction transaction, List<TransactionLedgerEntry> ledgerEntries);
        Task<int> SaveChangesAsync();
    }
}
