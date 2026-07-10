using BankingApi.Application.Interfaces.Repositories;
using BankingApi.Domain.Entities;
using BankingApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BankingApi.Infrastructure.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly ApplicationDbContext _context;

        public PaymentRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Payment?> GetByReferenceAsync(string reference)
        {
            return await _context.Payments
                .FirstOrDefaultAsync(p => p.Reference == reference);
        }

        public async Task<Payment?> GetByPaystackReferenceAsync(string paystackReference)
        {
            return await _context.Payments
                .FirstOrDefaultAsync(p => p.PaystackReference == paystackReference);
        }

        public async Task AddAsync(Payment payment)
        {
            await _context.Payments.AddAsync(payment);
        }

        public Task UpdateAsync(Payment payment)
        {
            _context.Payments.Update(payment);
            return Task.CompletedTask;
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
