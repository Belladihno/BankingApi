using BankingApi.Application.Interfaces.Repositories;
using BankingApi.Domain.Entities;
using BankingApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BankingApi.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ApplicationUser?> GetByIdAsync(Guid id)
        {
            return await _context.ApplicationUsers
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<ApplicationUser?> GetByEmailAsync(string email)
        {
            return await _context.ApplicationUsers
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<List<ApplicationUser>> GetAllAsync(int page, int pageSize, bool? isActive)
        {
            var query = _context.ApplicationUsers.AsQueryable();

            if (isActive.HasValue)
                query = query.Where(u => u.IsActive == isActive.Value);

            return await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task AddAsync(ApplicationUser user)
        {
            await _context.ApplicationUsers.AddAsync(user);
        }

        public Task UpdateAsync(ApplicationUser user)
        {
            _context.ApplicationUsers.Update(user);
            return Task.CompletedTask;
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
