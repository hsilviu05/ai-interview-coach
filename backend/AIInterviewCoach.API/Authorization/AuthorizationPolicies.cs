using AIInterviewCoach.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace AIInterviewCoach.API.Authorization
{
    public static class AuthorizationPolicies
    {
        public const string InterviewerWorkspaceAccess = "InterviewerWorkspaceAccess";
        public const string CandidateWorkspaceAccess = "CandidateWorkspaceAccess";
        public const string AdminProblemManagement = "AdminProblemManagement";
        public const string AdminAuditLogAccess = "AdminAuditLogAccess";
        public const string AdminObservabilityAccess = "AdminObservabilityAccess";

        public static void Register(AuthorizationOptions options)
        {
            options.AddPolicy(InterviewerWorkspaceAccess, policy =>
                policy.RequireRole(UserRole.Interviewer.ToString(), UserRole.Admin.ToString()));

            options.AddPolicy(CandidateWorkspaceAccess, policy =>
                policy.RequireRole(UserRole.Candidate.ToString(), UserRole.Admin.ToString()));

            options.AddPolicy(AdminProblemManagement, policy =>
                policy.RequireRole(UserRole.Admin.ToString()));

            options.AddPolicy(AdminAuditLogAccess, policy =>
                policy.RequireRole(UserRole.Admin.ToString()));

            options.AddPolicy(AdminObservabilityAccess, policy =>
                policy.RequireRole(UserRole.Admin.ToString()));
        }
    }
}
