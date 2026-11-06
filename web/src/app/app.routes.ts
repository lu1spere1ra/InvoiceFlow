import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'documents' },
  {
    path: 'documents',
    title: 'Documents · InvoiceFlow',
    loadComponent: () => import('./features/invoices/invoice-list').then((m) => m.InvoiceList),
  },
  {
    path: 'documents/new',
    title: 'New document · InvoiceFlow',
    loadComponent: () => import('./features/invoices/invoice-editor').then((m) => m.InvoiceEditor),
  },
  {
    path: 'documents/:id/edit',
    title: 'Edit draft · InvoiceFlow',
    loadComponent: () => import('./features/invoices/invoice-editor').then((m) => m.InvoiceEditor),
  },
  {
    path: 'documents/:id',
    title: 'Document · InvoiceFlow',
    loadComponent: () => import('./features/invoices/invoice-detail').then((m) => m.InvoiceDetail),
  },
  {
    path: 'customers',
    title: 'Customers · InvoiceFlow',
    loadComponent: () => import('./features/customers/customer-list').then((m) => m.CustomerList),
  },
  {
    path: 'customers/new',
    title: 'New customer · InvoiceFlow',
    loadComponent: () => import('./features/customers/customer-form').then((m) => m.CustomerForm),
  },
  { path: '**', redirectTo: 'documents' },
];
