import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { MessageService } from 'primeng/api';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '@core/services/auth.service';

/** Fallback shown when a 500-class response has no safe message body. */
const GENERIC_SERVER_ERROR = '系統發生錯誤，請稍後再試。 An unexpected error occurred.';

/**
 * Attaches `Authorization: Bearer <token>` (when a token is in session storage) to every outgoing
 * request, and centralises HTTP error handling:
 *  - on any 401 from a protected call → clear the session and redirect to /login;
 *  - on any 500-class error → show a friendly error toast using the safe message from the response
 *    body (the backend's generic message), never the raw error.
 *
 * The login request is exempt from the 401 redirect: a bad-credentials 401 there must surface to the
 * Login page so it can show the error, not bounce the user around. Other statuses (e.g. validation
 * 400) pass straight through so forms can surface them.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const messages = inject(MessageService);

  const token = auth.token;
  const authedReq = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  const isLoginRequest = req.url.includes('/api/Auth/login');

  return next(authedReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 && !isLoginRequest) {
        auth.logout();
        void router.navigateByUrl('/login');
      } else if (error.status >= 500) {
        const detail =
          error.error && typeof error.error === 'object' && error.error.message
            ? error.error.message
            : GENERIC_SERVER_ERROR;
        messages.add({ severity: 'error', summary: '錯誤 Error', detail });
      }
      return throwError(() => error);
    }),
  );
};
