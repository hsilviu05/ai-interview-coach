import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const roleGuard: CanActivateFn = (route: ActivatedRouteSnapshot) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const allowedRoles = route.data['roles'] as string[] | undefined;
  const currentRole = authService.getRole();

  if (allowedRoles?.includes(currentRole ?? '')) {
    return true;
  }

  return router.createUrlTree(['/login']);
};