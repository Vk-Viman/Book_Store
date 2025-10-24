import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { catchError, switchMap } from 'rxjs/operators';
import { throwError } from 'rxjs';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = localStorage.getItem('token');

  let authReq = req;
  // Attach token for non-auth endpoints
  if (token && !req.url.endsWith('/auth/login') && !req.url.endsWith('/auth/register') && !req.url.endsWith('/auth/refresh')) {
    authReq = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  const router = inject(Router);
  const auth = inject(AuthService);

  return next(authReq).pipe(
    catchError(err => {
      if (err.status === 401) {
        // Try refresh flow
        const refresh = auth.getRefresh();
        if (refresh) {
          return auth.refreshToken().pipe(
            switchMap((res: any) => {
              auth.setSession(res.token, res.refresh);
              const retryReq = req.clone({ setHeaders: { Authorization: `Bearer ${res.token}` } });
              return next(retryReq);
            }),
            catchError(() => {
              auth.logout();
              router.navigate(['/login']);
              return throwError(() => err);
            })
          );
        }

        auth.logout();
        router.navigate(['/login']);
      }
      return throwError(() => err);
    })
  );
};
