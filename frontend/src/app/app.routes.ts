import { Routes } from '@angular/router';
import { authGuard } from './core/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    title: 'Sign in · Market Workplace',
    loadComponent: () =>
      import('./features/login/login.component').then((m) => m.LoginComponent),
  },
  {
    // Everything below requires a session; unauthenticated visits redirect to /login.
    path: '',
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        title: 'Dashboard · Market Workplace',
        data: { breadcrumb: 'Dashboard' },
        loadComponent: () =>
          import('./features/dashboard/dashboard.component').then((m) => m.DashboardComponent),
      },
      {
        path: 'products',
        title: 'Products · Market Workplace',
        data: { breadcrumb: 'Products' },
        loadComponent: () =>
          import('./features/products/products.component').then((m) => m.ProductsComponent),
      },
      {
        path: 'services',
        title: 'Services · Market Workplace',
        data: { breadcrumb: 'Services' },
        loadComponent: () =>
          import('./features/services/services.component').then((m) => m.ServicesComponent),
      },
      {
        path: 'orders',
        title: 'Orders · Market Workplace',
        data: { breadcrumb: 'Orders' },
        loadComponent: () =>
          import('./features/orders/orders.component').then((m) => m.OrdersComponent),
      },
      {
        path: 'users',
        title: 'Users · Market Workplace',
        data: { breadcrumb: 'Users' },
        loadComponent: () =>
          import('./features/users/users.component').then((m) => m.UsersComponent),
      },
      {
        path: 'roles',
        title: 'Roles · Market Workplace',
        data: { breadcrumb: 'Roles' },
        loadComponent: () =>
          import('./features/roles/roles.component').then((m) => m.RolesComponent),
      },
      { path: '**', redirectTo: 'dashboard' },
    ],
  },
  { path: '**', redirectTo: '' },
];
