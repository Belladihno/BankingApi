using BankingApi.Application.DTOs.Accounts;
using BankingApi.Application.Interfaces;
using BankingApi.Application.Interfaces.Repositories;
using BankingApi.Application.Services;
using BankingApi.Domain.Entities;
using BankingApi.Domain.Enums;
using BankingApi.Domain.Exceptions;
using FluentAssertions;
using Moq;

namespace BankingApi.Tests.Services;

public class AccountServiceTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IAccountRepository> _accountRepo = new();
    private readonly Mock<IAuditLogRepository> _auditRepo = new();
    private readonly AccountService _sut;

    public AccountServiceTests()
    {
        _sut = new AccountService(_accountRepo.Object, _userRepo.Object, _auditRepo.Object);
    }

    [Fact]
    public async Task OpenAccountAsync_CreatesAccount_WhenOwnerExists()
    {
        var ownerId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _userRepo.Setup(x => x.GetByIdAsync(ownerId)).ReturnsAsync(new ApplicationUser
        {
            Id = ownerId,
            FirstName = "John",
            LastName = "Doe"
        });
        _accountRepo.Setup(x => x.GetByAccountNumberAsync(It.IsAny<string>())).ReturnsAsync((Account?)null);

        var request = new OpenAccountRequest
        {
            OwnerId = ownerId,
            AccountType = AccountType.Savings,
            OpeningDeposit = 500
        };

        var result = await _sut.OpenAccountAsync(request, userId);

        result.AccountType.Should().Be(AccountType.Savings);
        result.Balance.Should().Be(500);
        result.AccountNumber.Should().NotBeNullOrEmpty();
        _accountRepo.Verify(x => x.AddAsync(It.IsAny<Account>()), Times.Once);
    }

    [Fact]
    public async Task OpenAccountAsync_Throws_WhenOwnerNotFound()
    {
        var ownerId = Guid.NewGuid();
        _userRepo.Setup(x => x.GetByIdAsync(ownerId)).ReturnsAsync((ApplicationUser?)null);

        var request = new OpenAccountRequest
        {
            OwnerId = ownerId,
            AccountType = AccountType.Savings,
            OpeningDeposit = 500
        };

        var act = () => _sut.OpenAccountAsync(request, Guid.NewGuid());

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Owner not found");
    }

    [Fact]
    public async Task OpenAccountAsync_SetsSavingsLimit_ForSavingsAccount()
    {
        var ownerId = Guid.NewGuid();
        _userRepo.Setup(x => x.GetByIdAsync(ownerId)).ReturnsAsync(new ApplicationUser
        {
            Id = ownerId,
            FirstName = "John",
            LastName = "Doe"
        });
        _accountRepo.Setup(x => x.GetByAccountNumberAsync(It.IsAny<string>())).ReturnsAsync((Account?)null);

        var request = new OpenAccountRequest
        {
            OwnerId = ownerId,
            AccountType = AccountType.Savings,
            OpeningDeposit = 500
        };

        var result = await _sut.OpenAccountAsync(request, Guid.NewGuid());

        result.DailyWithdrawalLimit.Should().Be(200000);
    }

    [Fact]
    public async Task OpenAccountAsync_SetsCurrentLimit_ForCurrentAccount()
    {
        var ownerId = Guid.NewGuid();
        _userRepo.Setup(x => x.GetByIdAsync(ownerId)).ReturnsAsync(new ApplicationUser
        {
            Id = ownerId,
            FirstName = "John",
            LastName = "Doe"
        });
        _accountRepo.Setup(x => x.GetByAccountNumberAsync(It.IsAny<string>())).ReturnsAsync((Account?)null);

        var request = new OpenAccountRequest
        {
            OwnerId = ownerId,
            AccountType = AccountType.Current,
            OpeningDeposit = 500
        };

        var result = await _sut.OpenAccountAsync(request, Guid.NewGuid());

        result.DailyWithdrawalLimit.Should().Be(500000);
    }

    [Fact]
    public async Task OpenAccountAsync_Throws_WhenDepositBelowMinimum()
    {
        var ownerId = Guid.NewGuid();
        _userRepo.Setup(x => x.GetByIdAsync(ownerId)).ReturnsAsync(new ApplicationUser
        {
            Id = ownerId,
            FirstName = "John"
        });

        var request = new OpenAccountRequest
        {
            OwnerId = ownerId,
            AccountType = AccountType.Savings,
            OpeningDeposit = 50
        };

        var act = () => _sut.OpenAccountAsync(request, Guid.NewGuid());

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Minimum opening deposit is *");
    }

    [Fact]
    public async Task GetAccountsByOwnerAsync_ReturnsAccounts_ForValidOwner()
    {
        var ownerId = Guid.NewGuid();
        var accounts = new List<Account>
        {
            new() { Id = Guid.NewGuid(), OwnerId = ownerId, Balance = 1000, AccountNumber = "0580000001" },
            new() { Id = Guid.NewGuid(), OwnerId = ownerId, Balance = 2000, AccountNumber = "0580000002" }
        };
        _accountRepo.Setup(x => x.GetByOwnerIdAsync(ownerId)).ReturnsAsync(accounts);

        var result = await _sut.GetAccountsByOwnerAsync(ownerId);

        result.Should().HaveCount(2);
    }
}
