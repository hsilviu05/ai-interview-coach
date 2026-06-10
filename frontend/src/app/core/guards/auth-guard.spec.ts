import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, CanActivateFn, RouterStateSnapshot, UrlTree } from '@angular/router';
import { provideRouter } from '@angular/router';
import { vi } from 'vitest';

import { authGuard } from './auth-guard';
import { AuthService } from '../services/auth.service';

describe('authGuard', () => {
  let authServiceMock: { isAuthenticated: ReturnType<typeof vi.fn> };

  const executeGuard: CanActivateFn = (...guardParameters) =>
    TestBed.runInInjectionContext(() => authGuard(...guardParameters));

  beforeEach(() => {
    authServiceMock = {
      isAuthenticated: vi.fn(),
    };

    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authServiceMock },
      ],
    });
  });

  it('returns true when the user is authenticated', () => {
    authServiceMock.isAuthenticated.mockReturnValue(true);

    const result = executeGuard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot);

    expect(result).toBe(true);
  });

  it('redirects to /login when the user is not authenticated', () => {
    authServiceMock.isAuthenticated.mockReturnValue(false);

    const result = executeGuard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot);

    expect(result).toBeInstanceOf(UrlTree);
    expect((result as UrlTree).toString()).toBe('/login');
  });
});
