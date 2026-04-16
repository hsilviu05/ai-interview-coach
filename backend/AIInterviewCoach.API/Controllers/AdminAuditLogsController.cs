using AIInterviewCoach.API.Authorization;
using AIInterviewCoach.Application.DTOs.AdminAuditLogs;
using AIInterviewCoach.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace AIInterviewCoach.API.Controllers
{
    [ApiController]
    [Route("api/admin/audit-logs")]
    [Authorize(Policy = AuthorizationPolicies.AdminAuditLogAccess)]
    public class AdminAuditLogsController : ControllerBase
    {
        private readonly IAdminAuditService _adminAuditService;
        private readonly ILogger<AdminAuditLogsController> _logger;

        public AdminAuditLogsController(
            IAdminAuditService adminAuditService,
            ILogger<AdminAuditLogsController> logger)
        {
            _adminAuditService = adminAuditService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetRecentLogs([FromQuery] int take = 20, CancellationToken cancellationToken = default)
        {
            try
            {
                var logs = await _adminAuditService.GetRecentLogsAsync(take, cancellationToken);
                return Ok(logs);
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)
            {
                _logger.LogWarning(
                    ex,
                    "Admin audit log table is not available yet. Returning an empty audit log list until the backend restarts and applies migrations.");

                return Ok(Array.Empty<AdminAuditLogResponseDto>());
            }
        }
    }
}
