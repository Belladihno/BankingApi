namespace BankingApi.Application.Interfaces
{
    public interface ITokenService
    {
        string GenerateAccessToken(Guid userId, string email, string role);
        string GenerateRefreshToken();
        Task<bool> ValidateRefreshTokenAsync(Guid userId, string refreshToken);
        Task RevokeRefreshTokenAsync(Guid userId);
    }
}
