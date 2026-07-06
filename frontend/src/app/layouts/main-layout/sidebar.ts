import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

interface NavItem {
  path: string;
  label: string;
  icon: string;
  adminOnly?: boolean;
  userOnly?: boolean;
}

@Component({
  selector: 'app-sidebar',
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './sidebar.html',
  styleUrl: './sidebar.css',
})
export class Sidebar {
navItems: NavItem[] = [
  { path: '/dashboard', label: 'Dashboard', icon: 'dashboard', userOnly: true },
  { path: '/cv', label: 'CV Analysis', icon: 'description', userOnly: true },
  { path: '/jobs', label: 'Job Matches', icon: 'work', userOnly: true },
  { path: '/roadmap', label: 'Career Roadmap', icon: 'route', userOnly: true },
  { path: '/interview', label: 'Interview Lab', icon: 'record_voice_over', userOnly: true },
  { path: '/skills', label: 'Skills Gap', icon: 'assessment', userOnly: true },
  { path: '/profile', label: 'Profile', icon: 'person', userOnly: true },

  {
    path: '/admin-dashboard',
    label: 'Admin Dashboard',
    icon: 'admin_panel_settings',
    adminOnly: true
  },

  {
    path: '/admin/payments',
    label: 'Payments',
    icon: 'payments',
    adminOnly: true
}
];

  adminNavItems: NavItem[] = [
    { path: '/admin/jobs', label: 'Jobs Management', icon: 'admin_panel_settings' },
  ];

  constructor(public authService: AuthService) {}
}
