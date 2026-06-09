import { Routes } from '@angular/router';
import { Login } from './features/auth/login/login';
import { Register } from './features/auth/register/register';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
    { path: '', redirectTo: '/login', pathMatch: 'full' },
    { path: 'login', component: Login },
    { path: 'register', component: Register },
    { path: 'dashboard', loadComponent:
         () => import('./features/dashboard/dashboard')
      .then(m => m.Dashboard),
    canActivate: [authGuard] 
    },
    { path: '**', redirectTo: '/login' }
   
];
