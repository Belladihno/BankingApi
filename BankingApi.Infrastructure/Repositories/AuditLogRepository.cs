using BankingApi.Application.Interfaces.Repositories;
using BankingApi.Domain.Entities;
using BankingApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BankingApi.Infrastructure.Repositories
{
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly ApplicationDbContext _context;

        public AuditLogRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AuditLog?> GetByIdAsync(Guid id)
        {
            return await _context.AuditLogs
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<List<AuditLog>> GetAllAsync(string? entityName, string? entityId, string? actorId, DateTimeOffset? startDate, DateTimeOffset? endDate, int page, int pageSize)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrEmpty(entityName))
                query = query.Where(a => a.EntityName == entityName);

            if (!string.IsNullOrEmpty(entityId))
                query = query.Where(a => a.EntityId == entityId);

            if (!string.IsNullOrEmpty(actorId))
                query = query.Where(a => a.ActorId == actorId);

            if (startDate.HasValue)
                query = query.Where(a => a.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(a => a.CreatedAt <= endDate.Value);

            return await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task AddAsync(AuditLog auditLog)
        {
            await _context.AuditLogs.AddAsync(auditLog);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
