using BankingApi.Application.DTOs.Accounts;
using BankingApi.Application.Interfaces;
using BankingApi.Application.Interfaces.Repositories;
using BankingApi.Domain.Entities;
using BankingApi.Domain.Exceptions;
using BankingApi.Domain.Enums;

namespace BankingApi.Application.Services
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IUserRepository _userRepository;
        private readonly IAuditLogRepository _auditLogRepository;

        public AccountService(IAccountRepository accountRepository, IUserRepository userRepository, IAuditLogRepository auditLogRepository)
        {
            _accountRepository = accountRepository;
            _userRepository = userRepository;
            _auditLogRepository = auditLogRepository;
        }

        public async Task<AccountResponse> OpenAccountAsync(OpenAccountRequest request, Guid initiatedBy)
        {
            if (request.OpeningDeposit < 500.00m)
            {
                throw new DomainException("Minimum opening deposit is ₦500.00", "MIN_DEPOSIT");
            }

            var owner = await _userRepository.GetByIdAsync(request.OwnerId);
            if (owner == null)
            {
                throw new DomainException("Owner not found", "OWNER_NOT_FOUND");
            }

            var account = new Account
            {
                Id = Guid.NewGuid(),
                AccountNumber = await GenerateAccountNumberAsync(),
                AccountType = request.AccountType,
                Balance = request.OpeningDeposit,
                Status = AccountStatus.Active,
                DailyWithdrawalLimit = request.AccountType == AccountType.Savings ? 200000.00m : 500000.00m,
                TodayWithdrawnAmount = 0,
                OwnerId = request.OwnerId,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            await _accountRepository.AddAsync(account);
            await _accountRepository.SaveChangesAsync();

            await _auditLogRepository.AddAsync(new AuditLog
            {
                Id = Guid.NewGuid(),
                EntityName = "Account",
                EntityId = account.Id.ToString(),
                Action = "Created",
                ActorId = initiatedBy.ToString(),
                NewValues = $"{{ \"AccountNumber\": \"{account.AccountNumber}\", \"Balance\": {account.Balance} }}",
                CreatedAt = DateTimeOffset.UtcNow
            });
            await _auditLogRepository.SaveChangesAsync();

            return MapToResponse(account, $"{owner.FirstName} {owner.LastName}");
        }

        public async Task<AccountResponse> GetAccountByIdAsync(Guid accountId, Guid userId, string role)
        {
            var account = await _accountRepository.GetByIdAsync(accountId);
            if (account == null)
            {
                throw new DomainException("Account not found", "ACCOUNT_NOT_FOUND");
            }

            if (role == UserRole.Customer && account.OwnerId != userId)
            {
                throw new DomainException("You can only access your own accounts", "FORBIDDEN");
            }

            var owner = await _userRepository.GetByIdAsync(account.OwnerId);
            var ownerName = owner != null ? $"{owner.FirstName} {owner.LastName}" : "Unknown";

            return MapToResponse(account, ownerName);
        }

        public async Task<AccountResponse> GetAccountByNumberAsync(string accountNumber)
        {
            var account = await _accountRepository.GetByAccountNumberAsync(accountNumber);
            if (account == null)
            {
                throw new DomainException("Account not found", "ACCOUNT_NOT_FOUND");
            }

            var owner = await _userRepository.GetByIdAsync(account.OwnerId);
            var ownerName = owner != null ? $"{owner.FirstName} {owner.LastName}" : "Unknown";

            return MapToResponse(account, ownerName);
        }

        public async Task<List<AccountSummaryResponse>> GetAccountsByOwnerAsync(Guid ownerId)
        {
            var accounts = await _accountRepository.GetByOwnerIdAsync(ownerId);
            return accounts.Select(a => new AccountSummaryResponse
            {
                Id = a.Id,
                AccountNumber = a.AccountNumber,
                AccountType = a.AccountType,
                Balance = a.Balance,
                Status = a.Status
            }).ToList();
        }

        public async Task<List<AccountResponse>> GetAllAccountsAsync(int page, int pageSize, AccountStatus? status, AccountType? accountType)
        {
            var accounts = await _accountRepository.GetAllAsync(page, pageSize, status, accountType);
            var result = new List<AccountResponse>();

            foreach (var account in accounts)
            {
                var owner = await _userRepository.GetByIdAsync(account.OwnerId);
                var ownerName = owner != null ? $"{owner.FirstName} {owner.LastName}" : "Unknown";
                result.Add(MapToResponse(account, ownerName));
            }

            return result;
        }

        public async Task<AccountResponse> UpdateStatusAsync(Guid accountId, AccountStatus newStatus, string reason)
        {
            var account = await _accountRepository.GetByIdAsync(accountId);
            if (account == null)
            {
                throw new DomainException("Account not found", "ACCOUNT_NOT_FOUND");
            }

            var oldStatus = account.Status;
            account.Status = newStatus;
            account.UpdatedAt = DateTimeOffset.UtcNow;
            await _accountRepository.UpdateAsync(account);
            await _accountRepository.SaveChangesAsync();

            await _auditLogRepository.AddAsync(new AuditLog
            {
                Id = Guid.NewGuid(),
                EntityName = "Account",
                EntityId = account.Id.ToString(),
                Action = "StatusChanged",
                ActorId = "Admin",
                OldValues = $"{{ \"Status\": \"{oldStatus}\" }}",
                NewValues = $"{{ \"Status\": \"{newStatus}\", \"Reason\": \"{reason}\" }}",
                CreatedAt = DateTimeOffset.UtcNow
            });
            await _auditLogRepository.SaveChangesAsync();

            var owner = await _userRepository.GetByIdAsync(account.OwnerId);
            var ownerName = owner != null ? $"{owner.FirstName} {owner.LastName}" : "Unknown";

            return MapToResponse(account, ownerName);
        }

        public async Task<AccountResponse> UpdateDailyLimitAsync(Guid accountId, decimal newLimit)
        {
            var account = await _accountRepository.GetByIdAsync(accountId);
            if (account == null)
            {
                throw new DomainException("Account not found", "ACCOUNT_NOT_FOUND");
            }

            if (newLimit < 0)
            {
                throw new DomainException("Daily withdrawal limit cannot be negative", "INVALID_LIMIT");
            }

            account.DailyWithdrawalLimit = newLimit;
            account.UpdatedAt = DateTimeOffset.UtcNow;
            await _accountRepository.UpdateAsync(account);
            await _accountRepository.SaveChangesAsync();

            var owner = await _userRepository.GetByIdAsync(account.OwnerId);
            var ownerName = owner != null ? $"{owner.FirstName} {owner.LastName}" : "Unknown";

            return MapToResponse(account, ownerName);
        }

        private async Task<string> GenerateAccountNumberAsync()
        {
            const string prefix = "058";
            var random = new Random();
            var suffix = random.Next(0, 9999999).ToString("D7");
            return prefix + suffix;
        }

        private static AccountResponse MapToResponse(Account account, string ownerName)
        {
            return new AccountResponse
            {
                Id = account.Id,
                AccountNumber = account.AccountNumber,
                AccountType = account.AccountType,
                Balance = account.Balance,
                Status = account.Status,
                DailyWithdrawalLimit = account.DailyWithdrawalLimit,
                TodayWithdrawnAmount = account.TodayWithdrawnAmount,
                OwnerName = ownerName,
                CreatedAt = account.CreatedAt
            };
        }
    }
}
