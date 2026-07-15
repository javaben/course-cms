import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '@core/services/auth.service';

/**
 * Attaches `Authorization: Bearer <token>` (when a token is in session storage) to every outgoing
 * request, and — on any 401 from a protected call — clears the session and redirects to /login.
 *
 * The login request is exempt from the 401 redirect: a bad-credentials 401 there must surface to the
 * Login page so it can show the error, not bounce the user around.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);

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
      }
      return throwError(() => error);
    }),
  );
};
