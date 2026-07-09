using BankingApi.Domain.Entities;

namespace BankingApi.Application.Interfaces.Repositories
{
    public interface IUserRepository
    {
        Task<ApplicationUser?> GetByIdAsync(Guid id);
        Task<ApplicationUser?> GetByEmailAsync(string email);
        Task<List<ApplicationUser>> GetAllAsync(int page, int pageSize, bool? isActive);
        Task AddAsync(ApplicationUser user);
        Task UpdateAsync(ApplicationUser user);
        Task<int> SaveChangesAsync();
    }
}
