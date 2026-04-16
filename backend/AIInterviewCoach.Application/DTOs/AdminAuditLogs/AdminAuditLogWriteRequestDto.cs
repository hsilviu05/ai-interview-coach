namespace AIInterviewCoach.Application.DTOs.AdminAuditLogs
{
    public class AdminAuditLogWriteRequestDto
    {
        public Guid AdminUserId { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public string TargetType { get; set; } = string.Empty;
        public Guid? TargetId { get; set; }
        public string? TargetDisplayName { get; set; }
        public string Summary { get; set; } = string.Empty;
        public Dictionary<string, string>? Details { get; set; }
    }
}
