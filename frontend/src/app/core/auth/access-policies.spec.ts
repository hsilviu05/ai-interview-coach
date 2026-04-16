import {
  canAccessCandidateWorkspace,
  canAccessInterviewerWorkspace,
  canAccessPolicy,
  getDefaultRouteForRole,
  isAdminRole,
  routeAccessPolicies,
} from './access-policies';

describe('access policies', () => {
  it('should recognize the admin role', () => {
    expect(isAdminRole('Admin')).toBe(true);
    expect(isAdminRole('Candidate')).toBe(false);
  });

  it('should allow admins in both workspaces', () => {
    expect(canAccessInterviewerWorkspace('Admin')).toBe(true);
    expect(canAccessCandidateWorkspace('Admin')).toBe(true);
  });

  it('should enforce named route policies', () => {
    expect(canAccessPolicy('Interviewer', routeAccessPolicies.interviewerWorkspace)).toBe(true);
    expect(canAccessPolicy('Interviewer', routeAccessPolicies.adminProblemManagement)).toBe(false);
    expect(canAccessPolicy('Candidate', routeAccessPolicies.candidateWorkspace)).toBe(true);
  });

  it('should prefer the interviewer landing page for admins', () => {
    expect(getDefaultRouteForRole('Admin')).toBe('/interviewer/dashboard');
    expect(getDefaultRouteForRole('Candidate')).toBe('/candidate/access');
  });
});
