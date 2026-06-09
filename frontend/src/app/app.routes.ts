import { Routes } from '@angular/router';
import { AuthLayout } from './layouts/auth-layout/auth-layout';
import { MainLayout } from './layouts/main-layout/main-layout';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: '',
    component: AuthLayout,
    children: [
      { path: '', redirectTo: 'login', pathMatch: 'full' },
      { path: 'login', loadComponent: () => import('./features/auth/login/login').then(m => m.Login) },
      { path: 'register', loadComponent: () => import('./features/auth/register/register').then(m => m.Register) },
    ],
  },
  {
    path: '',
    component: MainLayout,
    canActivate: [authGuard],
    children: [
      { path: 'dashboard', loadComponent: () => import('./features/dashboard/dashboard').then(m => m.Dashboard) },
      { path: 'cv', loadComponent: () => import('./features/cv/cv').then(m => m.Cv) },
      { path: 'jobs', loadComponent: () => import('./features/jobs/jobs').then(m => m.Jobs) },
      { path: 'roadmap', loadComponent: () => import('./features/roadmap/roadmap').then(m => m.Roadmap) },
      { path: 'interview', loadComponent: () => import('./features/interview/interview').then(m => m.Interview) },
      { path: 'skills', loadComponent: () => import('./features/skills/skills').then(m => m.Skills) },
      { path: 'profile', loadComponent: () => import('./features/profile/profile').then(m => m.Profile) },
    ],
  },
  { path: '**', redirectTo: '/login' },
];
