import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import {
  CreateInvoiceDraftInput,
  Customer,
  CreditNoteInput,
  CustomerInput,
  Invoice,
  InvoiceDraftInput,
  Product,
  ProductInput,
} from './models';

/** Write operations. Reads use httpResource directly in components. */
@Injectable({ providedIn: 'root' })
export class Api {
  private readonly http = inject(HttpClient);

  createCustomer(input: CustomerInput) {
    return this.http.post<Customer>('/api/customers', input);
  }

  updateCustomer(id: string, input: CustomerInput) {
    return this.http.put<Customer>(`/api/customers/${id}`, input);
  }

  deleteCustomer(id: string) {
    return this.http.delete<void>(`/api/customers/${id}`);
  }

  createProduct(input: ProductInput) {
    return this.http.post<Product>('/api/products', input);
  }

  updateProduct(id: string, input: ProductInput) {
    return this.http.put<Product>(`/api/products/${id}`, input);
  }

  deleteProduct(id: string) {
    return this.http.delete<void>(`/api/products/${id}`);
  }

  createDraft(input: CreateInvoiceDraftInput) {
    return this.http.post<Invoice>('/api/invoices', input);
  }

  updateDraft(id: string, input: InvoiceDraftInput) {
    return this.http.put<Invoice>(`/api/invoices/${id}`, input);
  }

  deleteDraft(id: string) {
    return this.http.delete<void>(`/api/invoices/${id}`);
  }

  issue(id: string) {
    return this.http.post<Invoice>(`/api/invoices/${id}/issue`, null);
  }

  markPaid(id: string, paidOn: string | null) {
    return this.http.post<Invoice>(`/api/invoices/${id}/pay`, { paidOn });
  }

  void(id: string, reason: string) {
    return this.http.post<Invoice>(`/api/invoices/${id}/void`, { reason });
  }

  createCreditNote(id: string, input: CreditNoteInput) {
    return this.http.post<Invoice>(`/api/invoices/${id}/credit-notes`, input);
  }
}

/** Builds query params, skipping empty values. */
export function queryParams(
  values: Record<string, string | number | null | undefined>,
): HttpParams {
  let params = new HttpParams();
  for (const [key, value] of Object.entries(values)) {
    if (value !== null && value !== undefined && value !== '') {
      params = params.set(key, value);
    }
  }
  return params;
}
