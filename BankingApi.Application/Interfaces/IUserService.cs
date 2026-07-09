using BankingApi.Application.DTOs.Users;

namespace BankingApi.Application.Interfaces
{
    public interface IUserService
    {
        Task<List<UserResponse>> GetAllUsersAsync(int page, int pageSize, bool? isActive);
        Task<UserResponse> GetUserByIdAsync(Guid userId);
        Task DeactivateUserAsync(Guid userId);
        Task ActivateUserAsync(Guid userId);
        Task ResetPinAsync(Guid userId);
    }
}
