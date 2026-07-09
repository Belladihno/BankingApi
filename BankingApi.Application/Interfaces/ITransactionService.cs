using BankingApi.Application.DTOs.Transactions;
using BankingApi.Domain.Enums;

namespace BankingApi.Application.Interfaces
{
    public interface ITransactionService
    {
        Task<TransactionResponse> DepositAsync(DepositRequest request, Guid initiatedBy);
        Task<TransactionResponse> WithdrawAsync(WithdrawRequest request, Guid initiatedBy);
        Task<TransactionResponse> TransferAsync(TransferRequest request, Guid initiatedBy);
        Task<TransactionResponse> GetTransactionAsync(Guid transactionId, Guid userId, string role);
        Task<List<TransactionResponse>> GetAccountHistoryAsync(Guid accountId, Guid userId, string role, int page, int pageSize, TransactionType? type, DateTimeOffset? startDate, DateTimeOffset? endDate, TransactionStatus? status);
        Task<TransactionResponse> ReverseTransactionAsync(Guid transactionId, string reason, Guid initiatedBy);
    }
}
