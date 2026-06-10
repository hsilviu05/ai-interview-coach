import {
  canAccessCandidateWorkspace,
  canAccessInterviewerWorkspace,
  canAccessPolicy,
  getDefaultRouteForRole,
  isAdminRole,
  routeAccessPolicies,
} from './access-policies';

describe('access policies', () => {
  // ── isAdminRole ───────────────────────────────────────────────────────────

  it('recognizes Admin as the admin role', () => {
    expect(isAdminRole('Admin')).toBe(true);
  });

  it('rejects non-admin roles and null', () => {
    expect(isAdminRole('Candidate')).toBe(false);
    expect(isAdminRole('Interviewer')).toBe(false);
    expect(isAdminRole(null)).toBe(false);
  });

  // ── canAccessInterviewerWorkspace ─────────────────────────────────────────

  it('grants interviewer workspace access to Interviewer and Admin', () => {
    expect(canAccessInterviewerWorkspace('Interviewer')).toBe(true);
    expect(canAccessInterviewerWorkspace('Admin')).toBe(true);
  });

  it('denies interviewer workspace access to Candidate and null', () => {
    expect(canAccessInterviewerWorkspace('Candidate')).toBe(false);
    expect(canAccessInterviewerWorkspace(null)).toBe(false);
  });

  // ── canAccessCandidateWorkspace ───────────────────────────────────────────

  it('grants candidate workspace access to Candidate and Admin', () => {
    expect(canAccessCandidateWorkspace('Candidate')).toBe(true);
    expect(canAccessCandidateWorkspace('Admin')).toBe(true);
  });

  it('denies candidate workspace access to Interviewer and null', () => {
    expect(canAccessCandidateWorkspace('Interviewer')).toBe(false);
    expect(canAccessCandidateWorkspace(null)).toBe(false);
  });

  // ── canAccessPolicy ───────────────────────────────────────────────────────

  it('grants interviewer workspace policy to Interviewer and Admin only', () => {
    expect(canAccessPolicy('Interviewer', routeAccessPolicies.interviewerWorkspace)).toBe(true);
    expect(canAccessPolicy('Admin', routeAccessPolicies.interviewerWorkspace)).toBe(true);
    expect(canAccessPolicy('Candidate', routeAccessPolicies.interviewerWorkspace)).toBe(false);
    expect(canAccessPolicy(null, routeAccessPolicies.interviewerWorkspace)).toBe(false);
  });

  it('grants candidate workspace policy to Candidate and Admin only', () => {
    expect(canAccessPolicy('Candidate', routeAccessPolicies.candidateWorkspace)).toBe(true);
    expect(canAccessPolicy('Admin', routeAccessPolicies.candidateWorkspace)).toBe(true);
    expect(canAccessPolicy('Interviewer', routeAccessPolicies.candidateWorkspace)).toBe(false);
    expect(canAccessPolicy(null, routeAccessPolicies.candidateWorkspace)).toBe(false);
  });

  it('grants admin problem management policy to Admin only', () => {
    expect(canAccessPolicy('Admin', routeAccessPolicies.adminProblemManagement)).toBe(true);
    expect(canAccessPolicy('Interviewer', routeAccessPolicies.adminProblemManagement)).toBe(false);
    expect(canAccessPolicy('Candidate', routeAccessPolicies.adminProblemManagement)).toBe(false);
    expect(canAccessPolicy(null, routeAccessPolicies.adminProblemManagement)).toBe(false);
  });

  // ── getDefaultRouteForRole ────────────────────────────────────────────────

  it('sends Interviewer and Admin to the interviewer dashboard', () => {
    expect(getDefaultRouteForRole('Interviewer')).toBe('/interviewer/dashboard');
    expect(getDefaultRouteForRole('Admin')).toBe('/interviewer/dashboard');
  });

  it('sends Candidate and unauthenticated users to the candidate access page', () => {
    expect(getDefaultRouteForRole('Candidate')).toBe('/candidate/access');
    expect(getDefaultRouteForRole(null)).toBe('/candidate/access');
  });
});
