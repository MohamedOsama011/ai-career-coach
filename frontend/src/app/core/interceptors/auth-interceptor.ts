import { HttpInterceptorFn } from '@angular/common/http';
import { AuthService } from '../services/auth.service';
import { inject } from '@angular/core';

export const authInterceptor: HttpInterceptorFn = (req, next) => {

  const authService = inject(AuthService);

  const token = authService.getToken();

  if (token) {
    console.log('Auth token found, adding Authorization header to request.');
    req = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
  }else{
    console.warn('No auth token found, request will be sent without authentication header.');
  }

  return next(req);
};
