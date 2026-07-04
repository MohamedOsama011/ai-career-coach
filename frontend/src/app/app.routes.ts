import { Routes } from '@angular/router';
import { MainLayout } from './layouts/main-layout/main-layout';
import { authGuard } from './core/guards/auth.guard';
import { adminGuard } from './core/guards/admin.guard';
import { cvGuard } from './core/guards/cv.guard';
import { redirectIfLoggedIn } from './core/guards/redirect-if-logged-in.guard';
// import {Subscriptions}  from './features/subscription/subscriptions/subscriptions';
// import{CreateSubscription} from './features/subscription/create-subscription/create-subscription';
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
      { path: 'admin', canActivate: [adminGuard], children: [{ path: 'jobs', loadComponent: () => import('./features/admin/jobs-admin/jobs-admin').then(m => m.JobsAdmin) },]},
      {path:'subscriptions',loadComponent:()=>import('./features/subscription/subscriptions/subscriptions').then((as)=>as.Subscriptions),pathMatch: 'prefix'},
      {path:'create-subscription',loadComponent:()=>import('./features/subscription/create-subscription/create-subscription').then((c)=>c.CreateSubscription)},
      {path:'update-subscription/:id',loadComponent:()=>import('./features/subscription/update-subscription/update-subscription').then(u=>u.UpdateSubscriptionComponent)},
      {path:'view-subscription/:id',loadComponent:()=>import('./features/subscription/view-subscription/view-subscription').then(v=>v.ViewSubscriptionComponent)},

      {path:'usersubscriptions',loadComponent:()=>import('./features/usersubscriptions/usersubscription/usersubscription').then((as)=>as.Usersubscription),pathMatch: 'prefix'},
      {path:'payment-methods/:id',loadComponent:()=>import('./features/usersubscriptions/payment/payment').then((as)=>as.Payment)},


    ],},
  
  { path: '**', redirectTo: '' },
    
];
