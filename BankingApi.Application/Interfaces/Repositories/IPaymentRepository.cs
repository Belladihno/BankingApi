using BankingApi.Domain.Entities;

namespace BankingApi.Application.Interfaces.Repositories
{
    public interface IPaymentRepository
    {
        Task<Payment?> GetByReferenceAsync(string reference);
        Task<Payment?> GetByPaystackReferenceAsync(string paystackReference);
        Task AddAsync(Payment payment);
        Task UpdateAsync(Payment payment);
        Task<int> SaveChangesAsync();
    }
}
