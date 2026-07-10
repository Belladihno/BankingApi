using BankingApi.Domain.Enums;

namespace BankingApi.Domain.Entities
{
    public class Payment
    {
        public Guid Id { get; set; }
        public Guid AccountId { get; set; }
        public decimal Amount { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string? PaystackReference { get; set; }
        public string? PaystackAccessCode { get; set; }
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? CompletedAt { get; set; }

        public Account Account { get; set; } = null!;
    }
}
