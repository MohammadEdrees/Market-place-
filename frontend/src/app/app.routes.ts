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
        path: 'categories',
        title: 'Categories · Market Workplace',
        data: { breadcrumb: 'Categories' },
        loadComponent: () =>
          import('./features/categories/categories.component').then((m) => m.CategoriesComponent),
      },
      {
        path: 'advertisements',
        title: 'Advertisements · Market Workplace',
        data: { breadcrumb: 'Advertisements' },
        loadComponent: () =>
          import('./features/advertisements/advertisements.component').then((m) => m.AdvertisementsComponent),
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
      {
        path: 'subscriptions',
        title: 'Subscriptions · Market Workplace',
        data: { breadcrumb: 'Subscriptions' },
        loadComponent: () =>
          import('./features/subscriptions/subscriptions.component').then((m) => m.SubscriptionsComponent),
      },
      {
        path: 'audit-logs',
        title: 'Audit log · Market Workplace',
        data: { breadcrumb: 'Audit log' },
        loadComponent: () =>
          import('./features/audit-logs/audit-logs.component').then((m) => m.AuditLogsComponent),
      },
      {
        path: 'settings',
        title: 'Settings · Market Workplace',
        data: { breadcrumb: 'Settings' },
        loadComponent: () =>
          import('./features/settings/settings.component').then((m) => m.SettingsComponent),
      },
      { path: '**', redirectTo: 'dashboard' },
    ],
  },
  { path: '**', redirectTo: '' },
];
