using BankingApi.Application.DTOs.Transactions;
using BankingApi.Application.Interfaces;
using BankingApi.Application.Interfaces.Repositories;
using BankingApi.Domain.Entities;
using BankingApi.Domain.Exceptions;
using BankingApi.Domain.Enums;

namespace BankingApi.Application.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;

        public TransactionService(
            IAccountRepository accountRepository,
            ITransactionRepository transactionRepository,
            IAuditLogRepository auditLogRepository,
            IUserRepository userRepository,
            IPasswordHasher passwordHasher)
        {
            _accountRepository = accountRepository;
            _transactionRepository = transactionRepository;
            _auditLogRepository = auditLogRepository;
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
        }

        public async Task<TransactionResponse> DepositAsync(DepositRequest request, Guid initiatedBy)
        {
            var account = await _accountRepository.GetByIdAsync(request.AccountId);
            if (account == null)
            {
                throw new DomainException("Account not found", "ACCOUNT_NOT_FOUND");
            }

            if (account.Status == AccountStatus.Closed)
            {
                throw new AccountClosedException();
            }

            if (request.Amount < 100.00m)
            {
                throw new DomainException("Minimum deposit is ₦100.00", "MIN_AMOUNT");
            }

            if (request.Amount > 5000000.00m)
            {
                throw new DomainException("Maximum single transaction is ₦5,000,000.00", "MAX_AMOUNT");
            }

            account.Balance += request.Amount;
            account.UpdatedAt = DateTimeOffset.UtcNow;

            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                Reference = $"TXN-{Guid.NewGuid():N}"[..20].ToUpper(),
                Type = TransactionType.Deposit,
                Amount = request.Amount,
                DestinationAccountId = account.Id,
                SourceAccountId = null,
                Description = request.Description,
                Status = TransactionStatus.Completed,
                PinVerified = false,
                InitiatedBy = initiatedBy,
                CreatedAt = DateTimeOffset.UtcNow,
                CompletedAt = DateTimeOffset.UtcNow
            };

            var ledgerEntry = new TransactionLedgerEntry
            {
                Id = Guid.NewGuid(),
                TransactionId = transaction.Id,
                AccountId = account.Id,
                EntryType = LedgerEntryType.Credit,
                Amount = request.Amount,
                BalanceBefore = account.Balance - request.Amount,
                BalanceAfter = account.Balance,
                CreatedAt = DateTimeOffset.UtcNow
            };
            transaction.LedgerEntries.Add(ledgerEntry);

            await _transactionRepository.AddAsync(transaction);
            await _accountRepository.UpdateAsync(account);
            await _transactionRepository.AddLedgerEntriesAsync(transaction.LedgerEntries);
            await _transactionRepository.SaveChangesAsync();

            var initiator = await _userRepository.GetByIdAsync(initiatedBy);

            return MapToResponse(transaction, initiator?.FirstName ?? "Unknown");
        }

        public async Task<TransactionResponse> WithdrawAsync(WithdrawRequest request, Guid initiatedBy)
        {
            var account = await _accountRepository.GetByIdAsync(request.AccountId);
            if (account == null)
            {
                throw new DomainException("Account not found", "ACCOUNT_NOT_FOUND");
            }

            ValidateAccountForDebit(account);

            var user = await _userRepository.GetByIdAsync(initiatedBy);
            if (user == null)
            {
                throw new DomainException("User not found", "USER_NOT_FOUND");
            }

            VerifyPin(user, request.TransactionPin);

            if (request.Amount < 100.00m)
            {
                throw new DomainException("Minimum withdrawal is ₦100.00", "MIN_AMOUNT");
            }

            if (request.Amount > 5000000.00m)
            {
                throw new DomainException("Maximum single transaction is ₦5,000,000.00", "MAX_AMOUNT");
            }

            if (account.Balance < request.Amount)
            {
                throw new InsufficientFundsException($"Account balance of ₦{account.Balance:N} is insufficient for the requested ₦{request.Amount:N} withdrawal.");
            }

            if (account.TodayWithdrawnAmount + request.Amount > account.DailyWithdrawalLimit)
            {
                throw new DomainException($"Daily withdrawal limit of ₦{account.DailyWithdrawalLimit:N} would be exceeded", "DAILY_LIMIT");
            }

            account.Balance -= request.Amount;
            account.TodayWithdrawnAmount += request.Amount;
            account.UpdatedAt = DateTimeOffset.UtcNow;

            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                Reference = $"TXN-{Guid.NewGuid():N}"[..20].ToUpper(),
                Type = TransactionType.Withdrawal,
                Amount = request.Amount,
                SourceAccountId = account.Id,
                DestinationAccountId = null,
                Description = request.Description,
                Status = TransactionStatus.Completed,
                PinVerified = true,
                InitiatedBy = initiatedBy,
                CreatedAt = DateTimeOffset.UtcNow,
                CompletedAt = DateTimeOffset.UtcNow
            };

            var ledgerEntry = new TransactionLedgerEntry
            {
                Id = Guid.NewGuid(),
                TransactionId = transaction.Id,
                AccountId = account.Id,
                EntryType = LedgerEntryType.Debit,
                Amount = request.Amount,
                BalanceBefore = account.Balance + request.Amount,
                BalanceAfter = account.Balance,
                CreatedAt = DateTimeOffset.UtcNow
            };
            transaction.LedgerEntries.Add(ledgerEntry);

            await _transactionRepository.AddAsync(transaction);
            await _accountRepository.UpdateAsync(account);
            await _transactionRepository.AddLedgerEntriesAsync(transaction.LedgerEntries);
            await _transactionRepository.SaveChangesAsync();

            return MapToResponse(transaction, user.FirstName);
        }

        public async Task<TransactionResponse> TransferAsync(TransferRequest request, Guid initiatedBy)
        {
            if (request.SourceAccountId == request.DestinationAccountId)
            {
                throw new DomainException("Source and destination accounts must be different", "SAME_ACCOUNT");
            }

            var sourceAccount = await _accountRepository.GetByIdAsync(request.SourceAccountId);
            if (sourceAccount == null)
            {
                throw new DomainException("Source account not found", "ACCOUNT_NOT_FOUND");
            }

            var destinationAccount = await _accountRepository.GetByIdAsync(request.DestinationAccountId);
            if (destinationAccount == null)
            {
                throw new DomainException("Destination account not found", "ACCOUNT_NOT_FOUND");
            }

            ValidateAccountForDebit(sourceAccount);

            if (destinationAccount.Status == AccountStatus.Closed || destinationAccount.Status == AccountStatus.Frozen)
            {
                throw new DomainException("Destination account is not available for transfers", "DESTINATION_UNAVAILABLE");
            }

            var user = await _userRepository.GetByIdAsync(initiatedBy);
            if (user == null)
            {
                throw new DomainException("User not found", "USER_NOT_FOUND");
            }

            VerifyPin(user, request.TransactionPin);

            if (request.Amount < 100.00m)
            {
                throw new DomainException("Minimum transfer is ₦100.00", "MIN_AMOUNT");
            }

            if (request.Amount > 5000000.00m)
            {
                throw new DomainException("Maximum single transaction is ₦5,000,000.00", "MAX_AMOUNT");
            }

            if (sourceAccount.Balance < request.Amount)
            {
                throw new InsufficientFundsException($"Account balance of ₦{sourceAccount.Balance:N} is insufficient for the requested ₦{request.Amount:N} transfer.");
            }

            if (sourceAccount.TodayWithdrawnAmount + request.Amount > sourceAccount.DailyWithdrawalLimit)
            {
                throw new DomainException($"Daily withdrawal limit of ₦{sourceAccount.DailyWithdrawalLimit:N} would be exceeded", "DAILY_LIMIT");
            }

            sourceAccount.Balance -= request.Amount;
            sourceAccount.TodayWithdrawnAmount += request.Amount;
            sourceAccount.UpdatedAt = DateTimeOffset.UtcNow;

            destinationAccount.Balance += request.Amount;
            destinationAccount.UpdatedAt = DateTimeOffset.UtcNow;

            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                Reference = $"TXN-{Guid.NewGuid():N}"[..20].ToUpper(),
                Type = TransactionType.Transfer,
                Amount = request.Amount,
                SourceAccountId = sourceAccount.Id,
                DestinationAccountId = destinationAccount.Id,
                Description = request.Description,
                Status = TransactionStatus.Completed,
                PinVerified = true,
                InitiatedBy = initiatedBy,
                CreatedAt = DateTimeOffset.UtcNow,
                CompletedAt = DateTimeOffset.UtcNow
            };

            var debitEntry = new TransactionLedgerEntry
            {
                Id = Guid.NewGuid(),
                TransactionId = transaction.Id,
                AccountId = sourceAccount.Id,
                EntryType = LedgerEntryType.Debit,
                Amount = request.Amount,
                BalanceBefore = sourceAccount.Balance + request.Amount,
                BalanceAfter = sourceAccount.Balance,
                CreatedAt = DateTimeOffset.UtcNow
            };

            var creditEntry = new TransactionLedgerEntry
            {
                Id = Guid.NewGuid(),
                TransactionId = transaction.Id,
                AccountId = destinationAccount.Id,
                EntryType = LedgerEntryType.Credit,
                Amount = request.Amount,
                BalanceBefore = destinationAccount.Balance - request.Amount,
                BalanceAfter = destinationAccount.Balance,
                CreatedAt = DateTimeOffset.UtcNow
            };

            transaction.LedgerEntries.Add(debitEntry);
            transaction.LedgerEntries.Add(creditEntry);

            await _transactionRepository.ExecuteTransferAsync(sourceAccount, destinationAccount, transaction, transaction.LedgerEntries.ToList());

            return MapToResponse(transaction, user.FirstName);
        }

        public async Task<TransactionResponse> GetTransactionAsync(Guid transactionId, Guid userId, string role)
        {
            var transaction = await _transactionRepository.GetByIdAsync(transactionId);
            if (transaction == null)
            {
                throw new DomainException("Transaction not found", "TRANSACTION_NOT_FOUND");
            }

            if (role == UserRole.Customer)
            {
                var accounts = await _accountRepository.GetByOwnerIdAsync(userId);
                var accountIds = accounts.Select(a => a.Id).ToHashSet();
                var involved = (transaction.SourceAccountId.HasValue && accountIds.Contains(transaction.SourceAccountId.Value)) ||
                               (transaction.DestinationAccountId.HasValue && accountIds.Contains(transaction.DestinationAccountId.Value));
                if (!involved)
                {
                    throw new DomainException("You can only view transactions on your own accounts", "FORBIDDEN");
                }
            }

            var initiator = await _userRepository.GetByIdAsync(transaction.InitiatedBy);
            return MapToResponse(transaction, initiator?.FirstName ?? "Unknown");
        }

        public async Task<List<TransactionResponse>> GetAccountHistoryAsync(Guid accountId, Guid userId, string role, int page, int pageSize, TransactionType? type, DateTimeOffset? startDate, DateTimeOffset? endDate, TransactionStatus? status)
        {
            if (role == UserRole.Customer)
            {
                var account = await _accountRepository.GetByIdAsync(accountId);
                if (account == null || account.OwnerId != userId)
                {
                    throw new DomainException("You can only view your own account history", "FORBIDDEN");
                }
            }

            var transactions = await _transactionRepository.GetByAccountIdAsync(accountId, page, pageSize, type, startDate, endDate, status);
            var result = new List<TransactionResponse>();

            foreach (var t in transactions)
            {
                var initiator = await _userRepository.GetByIdAsync(t.InitiatedBy);
                result.Add(MapToResponse(t, initiator?.FirstName ?? "Unknown"));
            }

            return result;
        }

        public async Task<TransactionResponse> ReverseTransactionAsync(Guid transactionId, string reason, Guid initiatedBy)
        {
            var originalTransaction = await _transactionRepository.GetByIdAsync(transactionId);
            if (originalTransaction == null)
            {
                throw new DomainException("Transaction not found", "TRANSACTION_NOT_FOUND");
            }

            if (originalTransaction.Status != TransactionStatus.Completed)
            {
                throw new DomainException("Only completed transactions can be reversed", "NOT_COMPLETED");
            }

            var reversal = new Transaction
            {
                Id = Guid.NewGuid(),
                Reference = $"REV-{Guid.NewGuid():N}"[..20].ToUpper(),
                Type = originalTransaction.Type,
                Amount = originalTransaction.Amount,
                SourceAccountId = originalTransaction.DestinationAccountId,
                DestinationAccountId = originalTransaction.SourceAccountId,
                Description = $"Reversal: {reason}",
                Status = TransactionStatus.Completed,
                PinVerified = false,
                InitiatedBy = initiatedBy,
                CreatedAt = DateTimeOffset.UtcNow,
                CompletedAt = DateTimeOffset.UtcNow
            };

            if (originalTransaction.SourceAccountId.HasValue)
            {
                var sourceAccount = await _accountRepository.GetByIdAsync(originalTransaction.SourceAccountId.Value);
                if (sourceAccount != null)
                {
                    sourceAccount.Balance += originalTransaction.Amount;
                    sourceAccount.UpdatedAt = DateTimeOffset.UtcNow;
                    await _accountRepository.UpdateAsync(sourceAccount);
                }
            }

            if (originalTransaction.DestinationAccountId.HasValue)
            {
                var destAccount = await _accountRepository.GetByIdAsync(originalTransaction.DestinationAccountId.Value);
                if (destAccount != null)
                {
                    destAccount.Balance -= originalTransaction.Amount;
                    destAccount.UpdatedAt = DateTimeOffset.UtcNow;
                    await _accountRepository.UpdateAsync(destAccount);
                }
            }

            originalTransaction.Status = TransactionStatus.Reversed;

            await _transactionRepository.AddAsync(reversal);
            await _transactionRepository.SaveChangesAsync();

            var initiator = await _userRepository.GetByIdAsync(initiatedBy);
            return MapToResponse(reversal, initiator?.FirstName ?? "Unknown");
        }

        private void ValidateAccountForDebit(Account account)
        {
            if (account.Status == AccountStatus.Closed)
            {
                throw new AccountClosedException();
            }

            if (account.Status == AccountStatus.Frozen)
            {
                throw new AccountFrozenException();
            }
        }

        private void VerifyPin(ApplicationUser user, string pin)
        {
            if (user.PinLockedUntil.HasValue && user.PinLockedUntil.Value > DateTimeOffset.UtcNow)
            {
                throw new PinLockedException();
            }

            if (string.IsNullOrEmpty(user.TransactionPinHash))
            {
                throw new DomainException("Transaction PIN not set", "PIN_NOT_SET");
            }

            if (!_passwordHasher.Verify(pin, user.TransactionPinHash))
            {
                user.PinFailureCount++;
                if (user.PinFailureCount >= 3)
                {
                    user.PinLockedUntil = DateTimeOffset.UtcNow.AddMinutes(5);
                }

                throw new InvalidPinException();
            }

            user.PinFailureCount = 0;
            user.PinLockedUntil = null;
        }

        private static TransactionResponse MapToResponse(Transaction transaction, string initiatedByName)
        {
            return new TransactionResponse
            {
                Id = transaction.Id,
                Reference = transaction.Reference,
                Type = transaction.Type,
                Amount = transaction.Amount,
                SourceAccountId = transaction.SourceAccountId,
                DestinationAccountId = transaction.DestinationAccountId,
                Description = transaction.Description,
                Status = transaction.Status,
                PinVerified = transaction.PinVerified,
                InitiatedByName = initiatedByName,
                CreatedAt = transaction.CreatedAt,
                CompletedAt = transaction.CompletedAt,
                LedgerEntries = transaction.LedgerEntries.Select(e => new LedgerEntryResponse
                {
                    Id = e.Id,
                    AccountId = e.AccountId,
                    EntryType = e.EntryType,
                    Amount = e.Amount,
                    BalanceBefore = e.BalanceBefore,
                    BalanceAfter = e.BalanceAfter,
                    CreatedAt = e.CreatedAt
                }).ToList()
            };
        }
    }
}
