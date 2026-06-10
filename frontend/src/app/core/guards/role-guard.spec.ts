import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, CanActivateFn, RouterStateSnapshot, UrlTree } from '@angular/router';
import { provideRouter } from '@angular/router';
import { vi } from 'vitest';

import { roleGuard } from './role-guard';
import { AuthService } from '../services/auth.service';
import { routeAccessPolicies } from '../auth/access-policies';

describe('roleGuard', () => {
  let authServiceMock: { getRole: ReturnType<typeof vi.fn> };

  const executeGuard: CanActivateFn = (...guardParameters) =>
    TestBed.runInInjectionContext(() => roleGuard(...guardParameters));

  function makeRoute(data: Record<string, unknown>): ActivatedRouteSnapshot {
    return { data } as unknown as ActivatedRouteSnapshot;
  }

  beforeEach(() => {
    authServiceMock = { getRole: vi.fn() };

    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authServiceMock },
      ],
    });
  });

  it('returns true when no accessPolicy is set on the route', () => {
    authServiceMock.getRole.mockReturnValue('Candidate');

    const result = executeGuard(makeRoute({}), {} as RouterStateSnapshot);

    expect(result).toBe(true);
  });

  it('returns true when the user role satisfies the required policy', () => {
    authServiceMock.getRole.mockReturnValue('Interviewer');

    const result = executeGuard(
      makeRoute({ accessPolicy: routeAccessPolicies.interviewerWorkspace }),
      {} as RouterStateSnapshot
    );

    expect(result).toBe(true);
  });

  it('returns true when an Admin accesses any policy', () => {
    authServiceMock.getRole.mockReturnValue('Admin');

    for (const policy of Object.values(routeAccessPolicies)) {
      const result = executeGuard(makeRoute({ accessPolicy: policy }), {} as RouterStateSnapshot);
      expect(result).toBe(true);
    }
  });

  it('redirects to /login when the user is not authenticated', () => {
    authServiceMock.getRole.mockReturnValue(null);

    const result = executeGuard(
      makeRoute({ accessPolicy: routeAccessPolicies.interviewerWorkspace }),
      {} as RouterStateSnapshot
    );

    expect(result).toBeInstanceOf(UrlTree);
    expect((result as UrlTree).toString()).toBe('/login');
  });

  it('redirects a Candidate to their default route when accessing an interviewer-only area', () => {
    authServiceMock.getRole.mockReturnValue('Candidate');

    const result = executeGuard(
      makeRoute({ accessPolicy: routeAccessPolicies.interviewerWorkspace }),
      {} as RouterStateSnapshot
    );

    expect(result).toBeInstanceOf(UrlTree);
    expect((result as UrlTree).toString()).toBe('/candidate/access');
  });

  it('redirects an Interviewer to their default route when accessing a candidate-only area', () => {
    authServiceMock.getRole.mockReturnValue('Interviewer');

    const result = executeGuard(
      makeRoute({ accessPolicy: routeAccessPolicies.candidateWorkspace }),
      {} as RouterStateSnapshot
    );

    expect(result).toBeInstanceOf(UrlTree);
    expect((result as UrlTree).toString()).toBe('/interviewer/dashboard');
  });

  it('redirects a non-Admin away from admin-only areas', () => {
    authServiceMock.getRole.mockReturnValue('Interviewer');

    const result = executeGuard(
      makeRoute({ accessPolicy: routeAccessPolicies.adminProblemManagement }),
      {} as RouterStateSnapshot
    );

    expect(result).toBeInstanceOf(UrlTree);
    expect((result as UrlTree).toString()).toBe('/interviewer/dashboard');
  });
});
