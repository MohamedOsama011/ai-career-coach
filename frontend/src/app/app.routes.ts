import { Routes } from '@angular/router';
import { MainLayout } from './layouts/main-layout/main-layout';
import { authGuard } from './core/guards/auth.guard';
import { redirectIfLoggedIn } from './core/guards/redirect-if-logged-in.guard';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    canActivate: [redirectIfLoggedIn],
    loadComponent: () => import('./features/landing/landing').then(m => m.Landing),
  },
  {
    path: '',
    canActivate: [redirectIfLoggedIn],
    loadComponent: () => import('./layouts/auth-layout/auth-layout').then(m => m.AuthLayout),
    children: [
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
  { path: '**', redirectTo: '' },
];
