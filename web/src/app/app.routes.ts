import { Routes } from '@angular/router';
import { authGuard, writerGuard } from './core/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    title: 'Sign in · InvoiceFlow',
    loadComponent: () => import('./features/auth/login').then((m) => m.Login),
  },
  {
    path: '',
    canActivateChild: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'documents' },
      {
        path: 'documents',
        title: 'Documents · InvoiceFlow',
        loadComponent: () => import('./features/invoices/invoice-list').then((m) => m.InvoiceList),
      },
      {
        path: 'documents/new',
        canActivate: [writerGuard],
        title: 'New document · InvoiceFlow',
        loadComponent: () =>
          import('./features/invoices/invoice-editor').then((m) => m.InvoiceEditor),
      },
      {
        path: 'documents/:id/edit',
        canActivate: [writerGuard],
        title: 'Edit draft · InvoiceFlow',
        loadComponent: () =>
          import('./features/invoices/invoice-editor').then((m) => m.InvoiceEditor),
      },
      {
        path: 'documents/:id',
        title: 'Document · InvoiceFlow',
        loadComponent: () =>
          import('./features/invoices/invoice-detail').then((m) => m.InvoiceDetail),
      },
      {
        path: 'customers',
        title: 'Customers · InvoiceFlow',
        loadComponent: () =>
          import('./features/customers/customer-list').then((m) => m.CustomerList),
      },
      {
        path: 'customers/new',
        canActivate: [writerGuard],
        title: 'New customer · InvoiceFlow',
        loadComponent: () =>
          import('./features/customers/customer-form').then((m) => m.CustomerForm),
      },
    ],
  },
  { path: '**', redirectTo: 'documents' },
];
