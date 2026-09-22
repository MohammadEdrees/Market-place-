import { Routes } from '@angular/router';

export const routes: Routes = [
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
  { path: '**', redirectTo: 'dashboard' },
];
