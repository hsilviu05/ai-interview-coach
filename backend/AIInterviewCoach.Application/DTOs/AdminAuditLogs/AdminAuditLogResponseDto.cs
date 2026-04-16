namespace AIInterviewCoach.Application.DTOs.AdminAuditLogs
{
    public class AdminAuditLogResponseDto
    {
        public Guid Id { get; set; }
        public Guid AdminUserId { get; set; }
        public string AdminEmail { get; set; } = string.Empty;
        public string AdminFullName { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;
        public string TargetType { get; set; } = string.Empty;
        public Guid? TargetId { get; set; }
        public string? TargetDisplayName { get; set; }
        public string Summary { get; set; } = string.Empty;
        public string? DetailsJson { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
