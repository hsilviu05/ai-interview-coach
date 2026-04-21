using AIInterviewCoach.Domain.Entities;
using AIInterviewCoach.Domain.Enums;

namespace AIInterviewCoach.Application.Services
{
    internal static class ProblemAccessAuthorization
    {
        public static void EnsureProblemAccessible(
            Problem problem,
            Guid currentUserId,
            UserRole currentUserRole)
        {
            if (currentUserRole == UserRole.Admin)
                return;

            if (currentUserRole == UserRole.Candidate && !problem.IsPublic)
                throw new UnauthorizedAccessException("You do not have access to this problem.");

            if (currentUserRole == UserRole.Interviewer &&
                problem.CreatedByUserId != currentUserId &&
                !problem.IsPublic)
            {
                throw new UnauthorizedAccessException("You do not have access to this problem.");
            }
        }

        public static void EnsureProblemOwnership(Problem problem, Guid currentUserId, bool isAdmin)
        {
            if (!isAdmin && problem.CreatedByUserId != currentUserId)
                throw new UnauthorizedAccessException("You can only manage test cases for your own problems.");
        }
    }
}
