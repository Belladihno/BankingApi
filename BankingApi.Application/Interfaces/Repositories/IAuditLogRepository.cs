using BankingApi.Domain.Entities;

namespace BankingApi.Application.Interfaces.Repositories
{
    public interface IAuditLogRepository
    {
        Task<AuditLog?> GetByIdAsync(Guid id);
        Task<List<AuditLog>> GetAllAsync(string? entityName, string? entityId, string? actorId, DateTimeOffset? startDate, DateTimeOffset? endDate, int page, int pageSize);
        Task AddAsync(AuditLog auditLog);
        Task<int> SaveChangesAsync();
    }
}
