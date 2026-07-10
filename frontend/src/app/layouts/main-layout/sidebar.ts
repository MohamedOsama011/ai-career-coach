import { Component, inject, signal, input, output } from '@angular/core';
import { RouterLink, RouterLinkActive, Router, NavigationEnd } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { CareerProfileStore } from '../../core/store/career-profile-store';
import { filter } from 'rxjs/operators';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

interface NavItem {
  path: string;
  label: string;
  icon: string;
}

@Component({
  selector: 'app-sidebar',
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './sidebar.html',
  styleUrl: './sidebar.css',
})
export class Sidebar {
  readonly open = input(false);
  readonly closedrawer = output<void>();

  private router = inject(Router);
  readonly authService = inject(AuthService);
  readonly store = inject(CareerProfileStore);
  readonly isAdminMode = signal(false);

  navItems: NavItem[] = [
    { path: '/dashboard', label: 'Dashboard', icon: 'dashboard' },
    { path: '/cv', label: 'CV Analysis', icon: 'description' },
    { path: '/jobs', label: 'Job Matches', icon: 'work' },
    { path: '/roadmap', label: 'Career Roadmap', icon: 'route' },
    { path: '/interview', label: 'Interview Lab', icon: 'record_voice_over' },
    { path: '/skills', label: 'Skills Gap', icon: 'assessment' },
    { path: '/billing', label: 'Plans & Billing', icon: 'credit_card' },
    { path: '/profile', label: 'Profile', icon: 'person' },
  ];

  adminNavItems: NavItem[] = [
    { path: '/admin', label: 'Dashboard', icon: 'dashboard' },
    { path: '/admin/users', label: 'Users', icon: 'people' },
    { path: '/admin/jobs', label: 'Jobs', icon: 'work' },
    { path: '/admin/interviews', label: 'Interviews', icon: 'record_voice_over' },
    { path: '/admin/roadmap-templates', label: 'Roadmaps', icon: 'route' },
    { path: '/admin/plans', label: 'Plans', icon: 'card_membership' },
    { path: '/admin/subscribers', label: 'Subscribers', icon: 'group' },
    { path: '/admin/revenue', label: 'Revenue', icon: 'insights' },
    { path: '/admin/audit-log', label: 'Audit Log', icon: 'history' },
    { path: '/admin/reports', label: 'Reports', icon: 'bar_chart' },
    { path: '/admin/health', label: 'System Health', icon: 'monitor_heart' },
    { path: '/admin/chat', label: 'Chat Sessions', icon: 'chat' },
    { path: '/admin/broadcast', label: 'Broadcast', icon: 'campaign' },
  ];

  constructor() {
    this.isAdminMode.set(this.router.url.startsWith('/admin'));
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      takeUntilDestroyed()
    ).subscribe(e => {
      this.isAdminMode.set(e.url.startsWith('/admin'));
    });
  }
}
