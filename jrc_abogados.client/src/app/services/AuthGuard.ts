import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivateFn, Router } from '@angular/router';
import { AuthService } from './AuthService';

export const authGuard: CanActivateFn = (route: ActivatedRouteSnapshot, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  var user: any;

  if (!authService.isLoggedIn()) {
    router.navigate(['/login']);
    return false;
  }

  authService.usuario.subscribe(usuario => {
    user = usuario;
    if (!user) {
      router.navigate(['/login']);
    }
  });
  
  const restrictedRoutes = ['report-list', 'user-list'];

  if (restrictedRoutes.includes(route.routeConfig?.path || '')) {
    if (user.rolId != 1) {
      router.navigate(['/']);
      return false;
    }
  }

  return true;
};
