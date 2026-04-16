using AIInterviewCoach.Application.DTOs.AdminAuditLogs;

namespace AIInterviewCoach.Application.Interfaces.Services
{
    public interface IAdminAuditService
    {
        Task RecordAsync(AdminAuditLogWriteRequestDto request, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<AdminAuditLogResponseDto>> GetRecentLogsAsync(int take = 25, CancellationToken cancellationToken = default);
    }
}
