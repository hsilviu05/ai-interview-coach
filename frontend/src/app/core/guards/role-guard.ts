import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivateFn, Router } from '@angular/router';
import { canAccessPolicy, getDefaultRouteForRole, RouteAccessPolicy } from '../auth/access-policies';
import { AuthService } from '../services/auth.service';

export const roleGuard: CanActivateFn = (route: ActivatedRouteSnapshot) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const accessPolicy = route.data['accessPolicy'] as RouteAccessPolicy | undefined;
  const currentRole = authService.getRole();

  if (!accessPolicy || canAccessPolicy(currentRole, accessPolicy)) {
    return true;
  }

  if (!currentRole) {
    return router.createUrlTree(['/login']);
  }

  return router.createUrlTree([getDefaultRouteForRole(currentRole)]);
};
