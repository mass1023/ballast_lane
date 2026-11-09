import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  return authService.isLoggedIn() || inject(Router).createUrlTree(['/login']);
};

export const guestGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  return !authService.isLoggedIn() || inject(Router).createUrlTree(['/releases']);
};
