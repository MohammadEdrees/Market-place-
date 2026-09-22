import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';

/**
 * Sends `Authorization: Bearer <token>` with every API request. When the API rejects the
 * token (401) the session is cleared and the router takes the user back to /login —
 * except for the login call itself, whose 401 just means "wrong credentials".
 */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const token = auth.token();

  const authorized = token
    ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : request;

  return next(authorized).pipe(
    catchError((error) => {
      if (
        error instanceof HttpErrorResponse &&
        error.status === 401 &&
        !request.url.includes('/api/auth/login')
      ) {
        auth.logout();
      }
      return throwError(() => error);
    }),
  );
};
