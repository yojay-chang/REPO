import { inject } from '@angular/core';
import { CanActivateChildFn, CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

/** The forced change-password page — the only route a must-change-password user may open. */
export const CHANGE_PASSWORD_ROUTE = '/change-password';

/**
 * Blocks a route unless a token is present; otherwise redirects to the login page. A user who is still
 * on the system default password is additionally held on the change-password page — the backend enforces
 * the same rule on every endpoint, so letting them roam would only show them broken pages.
 */
export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.isAuthenticated()) return router.createUrlTree(['/login']);

  const onChangePasswordPage = (state?.url ?? '').startsWith(CHANGE_PASSWORD_ROUTE);

  if (auth.mustChangePassword())
    return onChangePasswordPage ? true : router.createUrlTree([CHANGE_PASSWORD_ROUTE]);

  // Nothing to change — the forced page has no purpose; send them to the app.
  return onChangePasswordPage ? router.createUrlTree(['/']) : true;
};

/** Same rule applied to child routes of the protected shell. */
export const authChildGuard: CanActivateChildFn = (route, state) => authGuard(route, state);
