import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map } from 'rxjs/operators';
import { of } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const cvGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return authService.getProfile().pipe(
    map(profile => {
      if ((profile?.cvCount ?? 0) > 0) return true;
      router.navigate(['/cv']);
      return false;
    }),
    catchError(() => of(true))
  );
};
