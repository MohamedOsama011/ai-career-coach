import { inject } from "@angular/core";
import { CanActivateFn, Router } from "@angular/router";
import { AuthService } from "../services/auth.service";

export const redirectIfLoggedIn: CanActivateFn = () => {

  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isLoggedIn()) {
    return true;
  }

  if (authService.getRoles().includes('Admin')) {
    return router.createUrlTree(['/admin']);
  }

  return router.createUrlTree(['/dashboard']);
};