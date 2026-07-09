using BankingApi.Application.DTOs.Auth;

namespace BankingApi.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterCustomerRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> RefreshTokenAsync(string refreshToken);
        Task SetupPinAsync(Guid userId, string pin);
        Task ChangePinAsync(Guid userId, string currentPin, string newPin);
        Task LogoutAsync(Guid userId, string refreshToken);
    }
}
