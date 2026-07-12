using BankingApi.Application.DTOs.Auth;
using BankingApi.Application.Interfaces;
using BankingApi.Application.Interfaces.Repositories;
using BankingApi.Application.Services;
using BankingApi.Domain.Entities;
using BankingApi.Domain.Exceptions;
using FluentAssertions;
using Moq;

namespace BankingApi.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _sut = new AuthService(_userRepo.Object, _tokenService.Object, _passwordHasher.Object);
    }

    [Fact]
    public async Task SetupPinAsync_SetsPinHash_WhenNoPinExists()
    {
        var userId = Guid.NewGuid();
        _userRepo.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(new ApplicationUser { Id = userId });
        _passwordHasher.Setup(x => x.Hash("1234")).Returns("hashedPin");

        await _sut.SetupPinAsync(userId, "1234");

        _userRepo.Verify(x => x.UpdateAsync(It.Is<ApplicationUser>(u => u.TransactionPinHash == "hashedPin")), Times.Once);
        _userRepo.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task SetupPinAsync_Throws_WhenPinAlreadySet()
    {
        var userId = Guid.NewGuid();
        _userRepo.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(new ApplicationUser
        {
            Id = userId,
            TransactionPinHash = "existingHash"
        });

        var act = () => _sut.SetupPinAsync(userId, "1234");

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Transaction PIN already set");
    }

    [Fact]
    public async Task SetupPinAsync_Throws_WhenPinIsNot4Digits()
    {
        var userId = Guid.NewGuid();
        _userRepo.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(new ApplicationUser { Id = userId });

        var act = () => _sut.SetupPinAsync(userId, "abc");

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("PIN must be exactly 4 numeric digits");
    }

    [Fact]
    public async Task LoginAsync_ReturnsTokens_WhenCredentialsValid()
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "test@test.com",
            PasswordHash = "hash",
            Role = "Customer"
        };
        _userRepo.Setup(x => x.GetByEmailAsync("test@test.com")).ReturnsAsync(user);
        _passwordHasher.Setup(x => x.Verify("password", "hash")).Returns(true);
        _tokenService.Setup(x => x.GenerateAccessToken(user.Id, user.Email, user.Role)).Returns("token");
        _tokenService.Setup(x => x.GenerateRefreshToken()).Returns("refresh");

        var result = await _sut.LoginAsync(new LoginRequest { Email = "test@test.com", Password = "password" });

        result.AccessToken.Should().Be("token");
        result.RefreshToken.Should().Be("refresh");
        result.Role.Should().Be("Customer");
    }

    [Fact]
    public async Task LoginAsync_Throws_WhenPasswordWrong()
    {
        _userRepo.Setup(x => x.GetByEmailAsync("test@test.com")).ReturnsAsync(new ApplicationUser
        {
            Email = "test@test.com",
            PasswordHash = "hash"
        });
        _passwordHasher.Setup(x => x.Verify("wrong", "hash")).Returns(false);

        var act = () => _sut.LoginAsync(new LoginRequest { Email = "test@test.com", Password = "wrong" });

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Invalid email or password");
    }
}
