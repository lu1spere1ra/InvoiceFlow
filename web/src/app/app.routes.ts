import { Routes } from '@angular/router';
import { adminGuard, authGuard, writerGuard } from './core/auth.guard';

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
      {
        path: '',
        pathMatch: 'full',
        title: 'Overview · InvoiceFlow',
        loadComponent: () => import('./features/dashboard/dashboard').then((m) => m.DashboardPage),
      },
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
        path: 'documents/:id/credit-note',
        canActivate: [writerGuard],
        title: 'New credit note · InvoiceFlow',
        loadComponent: () =>
          import('./features/invoices/credit-note-form').then((m) => m.CreditNoteForm),
      },
      {
        path: 'products',
        title: 'Products · InvoiceFlow',
        loadComponent: () => import('./features/products/product-list').then((m) => m.ProductList),
      },
      {
        path: 'saft',
        canActivate: [writerGuard],
        title: 'SAF-T export · InvoiceFlow',
        loadComponent: () => import('./features/saft/saft-export').then((m) => m.SaftExport),
      },
      {
        path: 'series',
        canActivate: [adminGuard],
        title: 'Series · InvoiceFlow',
        loadComponent: () => import('./features/series/series-list').then((m) => m.SeriesList),
      },
      {
        path: 'customers/new',
        canActivate: [writerGuard],
        title: 'New customer · InvoiceFlow',
        loadComponent: () =>
          import('./features/customers/customer-form').then((m) => m.CustomerForm),
      },
      {
        path: 'customers/:id',
        canActivate: [writerGuard],
        title: 'Edit customer · InvoiceFlow',
        loadComponent: () =>
          import('./features/customers/customer-form').then((m) => m.CustomerForm),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
