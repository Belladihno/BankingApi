using BankingApi.Application.DTOs.Transactions;
using BankingApi.Application.Interfaces;
using BankingApi.Application.Interfaces.Repositories;
using BankingApi.Application.Services;
using BankingApi.Domain.Entities;
using BankingApi.Domain.Enums;
using BankingApi.Domain.Exceptions;
using FluentAssertions;
using Moq;

namespace BankingApi.Tests.Services;

public class TransactionServiceTests
{
    private readonly Mock<IAccountRepository> _accountRepo = new();
    private readonly Mock<ITransactionRepository> _transactionRepo = new();
    private readonly Mock<IAuditLogRepository> _auditRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly TransactionService _sut;

    public TransactionServiceTests()
    {
        _sut = new TransactionService(
            _accountRepo.Object,
            _transactionRepo.Object,
            _auditRepo.Object,
            _userRepo.Object,
            _passwordHasher.Object);
    }

    [Fact]
    public async Task DepositAsync_IncreasesBalance_AndCreatesTransaction()
    {
        var accountId = Guid.NewGuid();
        var account = new Account
        {
            Id = accountId,
            Balance = 500,
            Status = AccountStatus.Active
        };
        _accountRepo.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);

        var result = await _sut.DepositAsync(new DepositRequest
        {
            AccountId = accountId,
            Amount = 300,
            Description = "Test deposit"
        }, Guid.NewGuid());

        result.Amount.Should().Be(300);
        result.Type.Should().Be(TransactionType.Deposit);
        account.Balance.Should().Be(800);
    }

    [Fact]
    public async Task WithdrawAsync_DeductsBalance_WhenPinIsValid()
    {
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var account = new Account
        {
            Id = accountId,
            OwnerId = userId,
            Balance = 1000,
            Status = AccountStatus.Active,
            DailyWithdrawalLimit = 200000,
            TodayWithdrawnAmount = 0,
            LastDailyResetDate = DateTimeOffset.UtcNow
        };
        var user = new ApplicationUser
        {
            Id = userId,
            TransactionPinHash = "hashedPin",
            PinFailureCount = 0
        };

        _accountRepo.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
        _userRepo.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
        _passwordHasher.Setup(x => x.Verify("1234", "hashedPin")).Returns(true);

        var result = await _sut.WithdrawAsync(new WithdrawRequest
        {
            AccountId = accountId,
            Amount = 300,
            TransactionPin = "1234"
        }, userId);

        result.Amount.Should().Be(300);
        result.Type.Should().Be(TransactionType.Withdrawal);
        account.Balance.Should().Be(700);
        account.TodayWithdrawnAmount.Should().Be(300);
    }

    [Fact]
    public async Task WithdrawAsync_Throws_WhenPinIsWrong()
    {
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var account = new Account
        {
            Id = accountId,
            OwnerId = userId,
            Balance = 1000,
            Status = AccountStatus.Active,
            DailyWithdrawalLimit = 200000
        };
        var user = new ApplicationUser
        {
            Id = userId,
            TransactionPinHash = "hashedPin",
            PinFailureCount = 0
        };

        _accountRepo.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
        _userRepo.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
        _passwordHasher.Setup(x => x.Verify("wrong", "hashedPin")).Returns(false);

        var act = () => _sut.WithdrawAsync(new WithdrawRequest
        {
            AccountId = accountId,
            Amount = 300,
            TransactionPin = "wrong"
        }, userId);

        await act.Should().ThrowAsync<InvalidPinException>();
    }

    [Fact]
    public async Task WithdrawAsync_Throws_WhenInsufficientBalance()
    {
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var account = new Account
        {
            Id = accountId,
            OwnerId = userId,
            Balance = 100,
            Status = AccountStatus.Active,
            DailyWithdrawalLimit = 200000
        };
        var user = new ApplicationUser
        {
            Id = userId,
            TransactionPinHash = "hashedPin",
            PinFailureCount = 0
        };

        _accountRepo.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
        _userRepo.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
        _passwordHasher.Setup(x => x.Verify("1234", "hashedPin")).Returns(true);

        var act = () => _sut.WithdrawAsync(new WithdrawRequest
        {
            AccountId = accountId,
            Amount = 300,
            TransactionPin = "1234"
        }, userId);

        await act.Should().ThrowAsync<InsufficientFundsException>();
    }

    [Fact]
    public async Task WithdrawAsync_Throws_WhenDailyLimitExceeded()
    {
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var account = new Account
        {
            Id = accountId,
            OwnerId = userId,
            Balance = 50000,
            Status = AccountStatus.Active,
            DailyWithdrawalLimit = 200000,
            TodayWithdrawnAmount = 190000,
            LastDailyResetDate = DateTimeOffset.UtcNow
        };
        var user = new ApplicationUser
        {
            Id = userId,
            TransactionPinHash = "hashedPin",
            PinFailureCount = 0
        };

        _accountRepo.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
        _userRepo.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
        _passwordHasher.Setup(x => x.Verify("1234", "hashedPin")).Returns(true);

        var act = () => _sut.WithdrawAsync(new WithdrawRequest
        {
            AccountId = accountId,
            Amount = 20000,
            TransactionPin = "1234"
        }, userId);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*daily withdrawal limit*");
    }

    [Fact]
    public async Task TransferAsync_MovesFunds_BetweenAccounts()
    {
        var sourceId = Guid.NewGuid();
        var destId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var source = new Account
        {
            Id = sourceId,
            OwnerId = userId,
            Balance = 1000,
            Status = AccountStatus.Active,
            DailyWithdrawalLimit = 200000,
            TodayWithdrawnAmount = 0,
            LastDailyResetDate = DateTimeOffset.UtcNow
        };
        var dest = new Account
        {
            Id = destId,
            Balance = 500,
            Status = AccountStatus.Active,
            AccountNumber = "0589999999"
        };
        var user = new ApplicationUser
        {
            Id = userId,
            TransactionPinHash = "hashedPin",
            PinFailureCount = 0
        };

        _accountRepo.Setup(x => x.GetByIdAsync(sourceId)).ReturnsAsync(source);
        _accountRepo.Setup(x => x.GetByIdAsync(destId)).ReturnsAsync(dest);
        _userRepo.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
        _passwordHasher.Setup(x => x.Verify("1234", "hashedPin")).Returns(true);
        _transactionRepo.Setup(x => x.ExecuteTransferAsync(It.IsAny<Account>(), It.IsAny<Account>(),
            It.IsAny<Transaction>(), It.IsAny<List<TransactionLedgerEntry>>()))
            .ReturnsAsync((Account s, Account d, Transaction t, List<TransactionLedgerEntry> _) => t);

        var result = await _sut.TransferAsync(new TransferRequest
        {
            SourceAccountId = sourceId,
            DestinationAccountId = destId,
            Amount = 300,
            TransactionPin = "1234"
        }, userId);

        result.Amount.Should().Be(300);
        result.Type.Should().Be(TransactionType.Transfer);
        source.Balance.Should().Be(700);
        dest.Balance.Should().Be(800);
    }

    [Fact]
    public async Task TransferAsync_Throws_WhenSourceAndDestSame()
    {
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var act = () => _sut.TransferAsync(new TransferRequest
        {
            SourceAccountId = accountId,
            DestinationAccountId = accountId,
            Amount = 100,
            TransactionPin = "1234"
        }, userId);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Source and destination accounts must be different");
    }

    [Fact]
    public async Task TransferAsync_Throws_WhenSourceInsufficient()
    {
        var sourceId = Guid.NewGuid();
        var destId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var source = new Account
        {
            Id = sourceId,
            OwnerId = userId,
            Balance = 50,
            Status = AccountStatus.Active,
            DailyWithdrawalLimit = 200000
        };
        var dest = new Account
        {
            Id = destId,
            Balance = 500,
            Status = AccountStatus.Active
        };
        var user = new ApplicationUser
        {
            Id = userId,
            TransactionPinHash = "hashedPin",
            PinFailureCount = 0
        };

        _accountRepo.Setup(x => x.GetByIdAsync(sourceId)).ReturnsAsync(source);
        _accountRepo.Setup(x => x.GetByIdAsync(destId)).ReturnsAsync(dest);
        _userRepo.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
        _passwordHasher.Setup(x => x.Verify("1234", "hashedPin")).Returns(true);

        var act = () => _sut.TransferAsync(new TransferRequest
        {
            SourceAccountId = sourceId,
            DestinationAccountId = destId,
            Amount = 300,
            TransactionPin = "1234"
        }, userId);

        await act.Should().ThrowAsync<InsufficientFundsException>();
    }

    [Fact]
    public async Task WithdrawAsync_ResetsDailyLimit_WhenNewDay()
    {
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var account = new Account
        {
            Id = accountId,
            OwnerId = userId,
            Balance = 50000,
            Status = AccountStatus.Active,
            DailyWithdrawalLimit = 200000,
            TodayWithdrawnAmount = 190000,
            LastDailyResetDate = DateTimeOffset.UtcNow.AddDays(-1)
        };
        var user = new ApplicationUser
        {
            Id = userId,
            TransactionPinHash = "hashedPin",
            PinFailureCount = 0
        };

        _accountRepo.Setup(x => x.GetByIdAsync(accountId)).ReturnsAsync(account);
        _userRepo.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);
        _passwordHasher.Setup(x => x.Verify("1234", "hashedPin")).Returns(true);

        await _sut.WithdrawAsync(new WithdrawRequest
        {
            AccountId = accountId,
            Amount = 5000,
            TransactionPin = "1234"
        }, userId);

        account.TodayWithdrawnAmount.Should().Be(5000);
        account.LastDailyResetDate.Date.Should().Be(DateTimeOffset.UtcNow.Date);
    }
}
