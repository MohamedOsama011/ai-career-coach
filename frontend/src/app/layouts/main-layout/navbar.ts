import { Component, inject, output } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { ThemeService } from '../../core/services/theme.service';
import { NotificationBell } from '../../shared/components/notification-bell/notification-bell';

@Component({
  selector: 'app-navbar',
  imports: [NotificationBell],
  templateUrl: './navbar.html',
  styleUrl: './navbar.css',
})
export class Navbar {
  readonly togglemenu = output<void>();

  private authService = inject(AuthService);
  private router = inject(Router);
  readonly themeService = inject(ThemeService);

  get isAdmin(): boolean {
    return this.authService.isAdmin();
  }

  get isAdminRoute(): boolean {
    return this.router.url.startsWith('/admin');
  }

  toggleAdmin(): void {
    if (this.isAdminRoute) {
      this.router.navigate(['/dashboard']);
    } else {
      this.router.navigate(['/admin']);
    }
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
