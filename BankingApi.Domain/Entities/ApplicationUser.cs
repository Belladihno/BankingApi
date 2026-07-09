using BankingApi.Domain.Enums;

namespace BankingApi.Domain.Entities
{
    public class ApplicationUser
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string NationalIdentityNumber { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = UserRole.Customer;
        public string? TransactionPinHash { get; set; }
        public int PinFailureCount { get; set; }
        public DateTimeOffset? PinLockedUntil { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
