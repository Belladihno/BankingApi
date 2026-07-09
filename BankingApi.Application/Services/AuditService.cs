using BankingApi.Application.Interfaces;
using BankingApi.Application.Interfaces.Repositories;
using BankingApi.Domain.Exceptions;

namespace BankingApi.Application.Services
{
    public class AuditService : IAuditService
    {
        private readonly IAuditLogRepository _auditLogRepository;

        public AuditService(IAuditLogRepository auditLogRepository)
        {
            _auditLogRepository = auditLogRepository;
        }

        public async Task<List<AuditLogResponse>> GetAuditLogsAsync(string? entityName, string? entityId, string? actorId, DateTimeOffset? startDate, DateTimeOffset? endDate, int page, int pageSize)
        {
            var logs = await _auditLogRepository.GetAllAsync(entityName, entityId, actorId, startDate, endDate, page, pageSize);
            return logs.Select(l => new AuditLogResponse
            {
                Id = l.Id,
                EntityName = l.EntityName,
                EntityId = l.EntityId,
                Action = l.Action,
                ActorId = l.ActorId,
                OldValues = l.OldValues,
                NewValues = l.NewValues,
                CreatedAt = l.CreatedAt
            }).ToList();
        }

        public async Task<AuditLogResponse> GetAuditLogByIdAsync(Guid auditLogId)
        {
            var log = await _auditLogRepository.GetByIdAsync(auditLogId);
            if (log == null)
            {
                throw new DomainException("Audit log not found", "AUDIT_LOG_NOT_FOUND");
            }

            return new AuditLogResponse
            {
                Id = log.Id,
                EntityName = log.EntityName,
                EntityId = log.EntityId,
                Action = log.Action,
                ActorId = log.ActorId,
                OldValues = log.OldValues,
                NewValues = log.NewValues,
                CreatedAt = log.CreatedAt
            };
        }
    }
}
