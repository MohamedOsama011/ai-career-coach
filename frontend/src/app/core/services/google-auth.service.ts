import { inject, Injectable, NgZone, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { AuthService } from './auth.service';
import { Router } from '@angular/router';
import { finalize } from 'rxjs/operators';

declare const google: any;

@Injectable({ providedIn: 'root' })
export class GoogleAuthService {
  private authService = inject(AuthService);
  private router = inject(Router);
  private zone = inject(NgZone);
  private platformId = inject(PLATFORM_ID);

  private readonly clientId = '452282372667-n2tinbfv11oqbsu61rkn2b0ir2tlh3o6.apps.googleusercontent.com';

  isLoading = false;

  initializeGoogleButton(elementId: string): void {
    if (!isPlatformBrowser(this.platformId)) return;

    if (typeof google !== 'undefined' && google.accounts) {
      this.initGoogle();
      return;
    }

    const script = document.createElement('script');
    script.src = 'https://accounts.google.com/gsi/client';
    script.async = true;
    script.defer = true;
    script.onload = () => this.initGoogle();
    document.body.appendChild(script);
  }

  private initGoogle(): void {
    if (typeof google === 'undefined' || !google.accounts) return;

    google.accounts.id.initialize({
      client_id: this.clientId,
      callback: (response: any) => this.handleCredentialResponse(response),
    });
  }

  triggerGoogleLogin(): void {
    if (typeof google === 'undefined' || !google.accounts) return;

    google.accounts.id.initialize({
      client_id: this.clientId,
      callback: (response: any) => this.handleCredentialResponse(response),
    });

    google.accounts.id.prompt();
  }

  private handleCredentialResponse(response: any): void {
    if (!response?.credential) return;

    this.isLoading = true;

    this.authService.googleLogin({ idToken: response.credential })
      .pipe(finalize(() => this.zone.run(() => (this.isLoading = false))))
      .subscribe({
        next: (authResponse) => {
          this.zone.run(() => {
            this.authService.saveToken(authResponse.token, true);
            this.authService.saveUserInfo(authResponse.fullName, authResponse.email, authResponse.roles);
            this.router.navigate(['/dashboard']);
          });
        },
        error: () => {
          this.zone.run(() => {
            console.error('Google login failed');
          });
        }
      });
  }
}
