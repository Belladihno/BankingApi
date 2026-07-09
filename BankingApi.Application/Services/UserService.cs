using BankingApi.Application.DTOs.Users;
using BankingApi.Application.Interfaces;
using BankingApi.Application.Interfaces.Repositories;
using BankingApi.Domain.Exceptions;

namespace BankingApi.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<List<UserResponse>> GetAllUsersAsync(int page, int pageSize, bool? isActive)
        {
            var users = await _userRepository.GetAllAsync(page, pageSize, isActive);
            return users.Select(u => new UserResponse
            {
                UserId = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                Role = u.Role,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt
            }).ToList();
        }

        public async Task<UserResponse> GetUserByIdAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new DomainException("User not found", "USER_NOT_FOUND");
            }

            return new UserResponse
            {
                UserId = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Role = user.Role,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            };
        }

        public async Task DeactivateUserAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new DomainException("User not found", "USER_NOT_FOUND");
            }

            user.IsActive = false;
            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();
        }

        public async Task ActivateUserAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new DomainException("User not found", "USER_NOT_FOUND");
            }

            user.IsActive = true;
            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();
        }

        public async Task ResetPinAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new DomainException("User not found", "USER_NOT_FOUND");
            }

            user.TransactionPinHash = null;
            user.PinFailureCount = 0;
            user.PinLockedUntil = null;
            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();
        }
    }
}
