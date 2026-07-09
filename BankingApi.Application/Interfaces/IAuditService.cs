namespace BankingApi.Application.Interfaces
{
    public interface IAuditService
    {
        Task<List<AuditLogResponse>> GetAuditLogsAsync(string? entityName, string? entityId, string? actorId, DateTimeOffset? startDate, DateTimeOffset? endDate, int page, int pageSize);
        Task<AuditLogResponse> GetAuditLogByIdAsync(Guid auditLogId);
    }

    public class AuditLogResponse
    {
        public Guid Id { get; set; }
        public string EntityName { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string ActorId { get; set; } = string.Empty;
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
