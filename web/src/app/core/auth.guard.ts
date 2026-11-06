import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Auth } from './auth';

export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(Auth);
  return auth.user()
    ? true
    : inject(Router).createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};

export const adminGuard: CanActivateFn = () => {
  const auth = inject(Auth);
  return auth.hasAnyRole('Admin') ? true : inject(Router).createUrlTree(['/documents']);
};

export const writerGuard: CanActivateFn = () => {
  const auth = inject(Auth);
  return auth.canWrite() ? true : inject(Router).createUrlTree(['/documents']);
};
