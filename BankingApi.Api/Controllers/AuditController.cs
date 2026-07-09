using BankingApi.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankingApi.Api.Controllers
{
    [ApiController]
    [Route("api/v1/audit")]
    [Authorize(Roles = "Admin")]
    public class AuditController : ControllerBase
    {
        private readonly IAuditService _auditService;

        public AuditController(IAuditService auditService)
        {
            _auditService = auditService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAuditLogs(
            [FromQuery] string? entityName = null,
            [FromQuery] string? entityId = null,
            [FromQuery] string? actorId = null,
            [FromQuery] DateTimeOffset? startDate = null,
            [FromQuery] DateTimeOffset? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await _auditService.GetAuditLogsAsync(entityName, entityId, actorId, startDate, endDate, page, pageSize);
            return Ok(new { success = true, data = result, timestamp = DateTimeOffset.UtcNow });
        }

        [HttpGet("{auditLogId}")]
        public async Task<IActionResult> GetAuditLogById(Guid auditLogId)
        {
            var result = await _auditService.GetAuditLogByIdAsync(auditLogId);
            return Ok(new { success = true, data = result, timestamp = DateTimeOffset.UtcNow });
        }
    }
}
