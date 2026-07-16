import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { MessageService } from 'primeng/api';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';

/** Toast key of the app-level `<p-toast>` used for global (interceptor-raised) errors. */
export const GLOBAL_TOAST_KEY = 'global';

/** Shown when a 500-class response carries no usable message from the server. */
const GENERIC_SERVER_ERROR = '系統發生錯誤，請稍後再試。An unexpected error occurred.';

/**
 * Attaches `Authorization: Bearer <token>` (when a token is in session storage) to every outgoing
 * request. On error:
 * - 401 → clears the session and redirects to the login page (unchanged).
 * - 500-class (>= 500) → surfaces a friendly error toast using the safe `message` from the response body.
 * Other statuses (e.g. validation 400/403) pass through untouched so the form/page handles them as before.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const messageService = inject(MessageService);

  const token = auth.token;
  const authReq = token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        auth.logout();
        void router.navigate(['/login']);
      } else if (error.status >= 500) {
        const detail =
          typeof error.error?.message === 'string' && error.error.message.trim()
            ? error.error.message
            : GENERIC_SERVER_ERROR;
        messageService.add({
          key: GLOBAL_TOAST_KEY,
          severity: 'error',
          summary: '系統錯誤 Error',
          detail,
        });
      }
      return throwError(() => error);
    }),
  );
};
