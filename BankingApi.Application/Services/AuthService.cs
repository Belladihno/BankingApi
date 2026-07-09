using BankingApi.Application.DTOs.Auth;
using BankingApi.Application.Interfaces;
using BankingApi.Application.Interfaces.Repositories;
using BankingApi.Domain.Entities;
using BankingApi.Domain.Exceptions;
using BankingApi.Domain.Enums;

namespace BankingApi.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly ITokenService _tokenService;
        private readonly IPasswordHasher _passwordHasher;

        public AuthService(IUserRepository userRepository, ITokenService tokenService, IPasswordHasher passwordHasher)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
            _passwordHasher = passwordHasher;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterCustomerRequest request)
        {
            var existingUser = await _userRepository.GetByEmailAsync(request.Email);
            if (existingUser != null)
            {
                throw new DomainException("Email is already registered", "EMAIL_EXISTS");
            }

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                FirstName = request.FirstName,
                LastName = request.LastName,
                NationalIdentityNumber = request.Nin,
                PhoneNumber = request.PhoneNumber,
                Email = request.Email,
                PasswordHash = _passwordHasher.Hash(request.Password),
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email, UserRole.Customer);
            var refreshToken = _tokenService.GenerateRefreshToken();

            return new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                TokenType = "Bearer",
                ExpiresIn = 900,
                UserId = user.Id,
                Email = user.Email,
                Role = UserRole.Customer
            };
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            {
                throw new DomainException("Invalid email or password", "INVALID_CREDENTIALS");
            }

            var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email, user.Role);
            var refreshToken = _tokenService.GenerateRefreshToken();

            return new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                TokenType = "Bearer",
                ExpiresIn = 900,
                UserId = user.Id,
                Email = user.Email,
                Role = user.Role
            };
        }

        public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
        {
            throw new NotImplementedException("Token validation will be implemented in Infrastructure layer");
        }

        public async Task SetupPinAsync(Guid userId, string pin)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new DomainException("User not found", "USER_NOT_FOUND");
            }

            if (!string.IsNullOrEmpty(user.TransactionPinHash))
            {
                throw new DomainException("Transaction PIN already set", "PIN_ALREADY_SET");
            }

            if (pin.Length != 4 || !pin.All(char.IsDigit))
            {
                throw new DomainException("PIN must be exactly 4 numeric digits", "INVALID_PIN_FORMAT");
            }

            user.TransactionPinHash = _passwordHasher.Hash(pin);
            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();
        }

        public async Task ChangePinAsync(Guid userId, string currentPin, string newPin)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new DomainException("User not found", "USER_NOT_FOUND");
            }

            if (string.IsNullOrEmpty(user.TransactionPinHash))
            {
                throw new DomainException("No PIN set. Set up a PIN first.", "PIN_NOT_SET");
            }

            if (!_passwordHasher.Verify(currentPin, user.TransactionPinHash))
            {
                throw new InvalidPinException();
            }

            if (newPin.Length != 4 || !newPin.All(char.IsDigit))
            {
                throw new DomainException("New PIN must be exactly 4 numeric digits", "INVALID_PIN_FORMAT");
            }

            user.TransactionPinHash = _passwordHasher.Hash(newPin);
            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();
        }

        public async Task LogoutAsync(Guid userId, string refreshToken)
        {
            await _tokenService.RevokeRefreshTokenAsync(userId);
        }
    }
}
