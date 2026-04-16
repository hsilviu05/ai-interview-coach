using System.Text.Json;
using AIInterviewCoach.Application.DTOs.AdminAuditLogs;
using AIInterviewCoach.Application.Interfaces.Services;
using AIInterviewCoach.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AIInterviewCoach.Application.Services
{
    public class AdminAuditService : IAdminAuditService
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private readonly IAppDbContext _dbContext;

        public AdminAuditService(IAppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task RecordAsync(AdminAuditLogWriteRequestDto request, CancellationToken cancellationToken = default)
        {
            var adminUser = await _dbContext.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(user => user.Id == request.AdminUserId, cancellationToken);

            if (adminUser is null)
            {
                throw new KeyNotFoundException("Admin user not found for audit logging.");
            }

            var auditLog = new AdminAuditLog
            {
                Id = Guid.NewGuid(),
                AdminUserId = adminUser.Id,
                AdminEmail = adminUser.Email,
                AdminFullName = adminUser.FullName,
                ActionType = request.ActionType.Trim(),
                TargetType = request.TargetType.Trim(),
                TargetId = request.TargetId,
                TargetDisplayName = request.TargetDisplayName?.Trim(),
                Summary = request.Summary.Trim(),
                DetailsJson = request.Details is { Count: > 0 }
                    ? JsonSerializer.Serialize(request.Details, JsonOptions)
                    : null,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.AdminAuditLogs.Add(auditLog);
        }

        public async Task<IReadOnlyList<AdminAuditLogResponseDto>> GetRecentLogsAsync(int take = 25, CancellationToken cancellationToken = default)
        {
            var safeTake = Math.Clamp(take, 1, 100);

            return await _dbContext.AdminAuditLogs
                .AsNoTracking()
                .OrderByDescending(log => log.CreatedAt)
                .Take(safeTake)
                .Select(log => new AdminAuditLogResponseDto
                {
                    Id = log.Id,
                    AdminUserId = log.AdminUserId,
                    AdminEmail = log.AdminEmail,
                    AdminFullName = log.AdminFullName,
                    ActionType = log.ActionType,
                    TargetType = log.TargetType,
                    TargetId = log.TargetId,
                    TargetDisplayName = log.TargetDisplayName,
                    Summary = log.Summary,
                    DetailsJson = log.DetailsJson,
                    CreatedAt = log.CreatedAt
                })
                .ToListAsync(cancellationToken);
        }
    }
}
