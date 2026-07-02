import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

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
  navItems: NavItem[] = [
    { path: '/dashboard', label: 'Dashboard', icon: 'dashboard' },
    { path: '/cv', label: 'CV Analysis', icon: 'description' },
    { path: '/jobs', label: 'Job Matches', icon: 'work' },
    { path: '/roadmap', label: 'Career Roadmap', icon: 'route' },
    { path: '/interview', label: 'Interview Lab', icon: 'record_voice_over' },
    { path: '/skills', label: 'Skills Gap', icon: 'assessment' },
    { path: '/profile', label: 'Profile', icon: 'person' },
  ];

  adminNavItems: NavItem[] = [
    { path: '/admin/jobs', label: 'Jobs Management', icon: 'admin_panel_settings' },
  ];

  constructor(public authService: AuthService) {}
}
