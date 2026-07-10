import { Routes } from '@angular/router';
import { MainLayout } from './layouts/main-layout/main-layout';
import { authGuard } from './core/guards/auth.guard';
import { adminGuard } from './core/guards/admin.guard';
import { cvGuard } from './core/guards/cv.guard';
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
      { path: 'forgot-password', loadComponent: () => import('./features/auth/forgot-password/forgot-password').then(m => m.ForgotPassword) },
      { path: 'reset-password', loadComponent: () => import('./features/auth/reset-password/reset-password').then(m => m.ResetPassword) },
    ],
  },
  {
    path: '',
    component: MainLayout,
    canActivate: [authGuard],
    children: [
      { path: 'dashboard', canActivate: [cvGuard], loadComponent: () => import('./features/dashboard/dashboard').then(m => m.Dashboard) },
      { path: 'cv', loadComponent: () => import('./features/cv/cv').then(m => m.Cv) },
      { path: 'jobs', canActivate: [cvGuard], loadComponent: () => import('./features/jobs/jobs').then(m => m.Jobs) },
      { path: 'roadmap', canActivate: [cvGuard], loadComponent: () => import('./features/roadmap/roadmap').then(m => m.Roadmap) },
      { path: 'interview', canActivate: [cvGuard], loadComponent: () => import('./features/interview').then(m => m.InterviewShell) },
      { path: 'skills', canActivate: [cvGuard], loadComponent: () => import('./features/skills/skills').then(m => m.Skills) },
      { path: 'profile', loadComponent: () => import('./features/profile/profile').then(m => m.Profile) },
      { path: 'billing', loadComponent: () => import('./features/subscription/billing/billing').then(m => m.Billing) },
      { path: 'subscriptions', redirectTo: '/billing' },
      { path: 'my-subscriptions', redirectTo: '/billing' },
      { path: 'payment/:id', loadComponent: () => import('./features/subscription/payment/payment').then(m => m.Payment) },
      { path: 'payment-history', loadComponent: () => import('./features/subscription/payment-history/payment-history').then(m => m.PaymentHistory) },
      { path: 'invoice/:paymentId', loadComponent: () => import('./features/subscription/invoice/invoice').then(m => m.Invoice) },
      { path: 'admin', canActivate: [adminGuard], children: [
        { path: '', pathMatch: 'full', loadComponent: () => import('./features/admin/admin-dashboard/admin-dashboard').then(m => m.AdminDashboard) },
        { path: 'jobs', loadComponent: () => import('./features/admin/jobs-admin/jobs-admin').then(m => m.JobsAdmin) },
        { path: 'roadmap-templates', loadComponent: () => import('./features/admin/roadmap-templates/roadmap-templates').then(m => m.RoadmapTemplates) },
        { path: 'users', loadComponent: () => import('./features/admin/users/users').then(m => m.AdminUsers) },
        { path: 'users/:id', loadComponent: () => import('./features/admin/user-detail/user-detail').then(m => m.UserDetail) },
        { path: 'plans', loadComponent: () => import('./features/admin/plans-management/plans-management').then(m => m.PlansManagement) },
        { path: 'subscribers', loadComponent: () => import('./features/admin/subscribers-management/subscribers-management').then(m => m.SubscribersManagement) },
        { path: 'subscribers/:id', loadComponent: () => import('./features/admin/subscriber-detail/subscriber-detail').then(m => m.SubscriberDetail) },
        { path: 'interviews', loadComponent: () => import('./features/admin/interview-admin/interview-admin').then(m => m.InterviewAdmin) },
        { path: 'revenue', loadComponent: () => import('./features/admin/revenue-dashboard/revenue-dashboard').then(m => m.RevenueDashboard) },
        { path: 'audit-log', loadComponent: () => import('./features/admin/audit-log/audit-log').then(m => m.AuditLog) },
        { path: 'reports', loadComponent: () => import('./features/admin/reports/reports').then(m => m.Reports) },
        { path: 'health', loadComponent: () => import('./features/admin/system-health/system-health').then(m => m.SystemHealth) },
        { path: 'chat', loadComponent: () => import('./features/admin/chat-admin/chat-admin').then(m => m.ChatAdmin) },
        { path: 'broadcast', loadComponent: () => import('./features/admin/broadcast/broadcast').then(m => m.Broadcast) },
      ]},
    ],
  },
  { path: '**', redirectTo: '' },
];
