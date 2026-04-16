export const routeAccessPolicies = {
  interviewerWorkspace: 'interviewerWorkspace',
  candidateWorkspace: 'candidateWorkspace',
  adminProblemManagement: 'adminProblemManagement',
} as const;

export type RouteAccessPolicy =
  (typeof routeAccessPolicies)[keyof typeof routeAccessPolicies];

export type AppRole = 'Candidate' | 'Interviewer' | 'Admin';

export function isAdminRole(role: string | null): role is AppRole {
  return role === 'Admin';
}

export function canAccessInterviewerWorkspace(role: string | null): boolean {
  return role === 'Interviewer' || isAdminRole(role);
}

export function canAccessCandidateWorkspace(role: string | null): boolean {
  return role === 'Candidate' || isAdminRole(role);
}

export function canAccessPolicy(role: string | null, policy: RouteAccessPolicy): boolean {
  switch (policy) {
    case routeAccessPolicies.interviewerWorkspace:
      return canAccessInterviewerWorkspace(role);
    case routeAccessPolicies.candidateWorkspace:
      return canAccessCandidateWorkspace(role);
    case routeAccessPolicies.adminProblemManagement:
      return isAdminRole(role);
    default:
      return false;
  }
}

export function getDefaultRouteForRole(role: string | null): string {
  if (canAccessInterviewerWorkspace(role)) {
    return '/interviewer/dashboard';
  }

  return '/candidate/access';
}
