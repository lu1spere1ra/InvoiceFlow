import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import {
  CreateInvoiceDraftInput,
  Customer,
  CustomerInput,
  Invoice,
  InvoiceDraftInput,
} from './models';

/** Write operations. Reads use httpResource directly in components. */
@Injectable({ providedIn: 'root' })
export class Api {
  private readonly http = inject(HttpClient);

  createCustomer(input: CustomerInput) {
    return this.http.post<Customer>('/api/customers', input);
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
}

/** Builds query params, skipping empty values. */
export function queryParams(values: Record<string, string | number | null | undefined>): HttpParams {
  let params = new HttpParams();
  for (const [key, value] of Object.entries(values)) {
    if (value !== null && value !== undefined && value !== '') {
      params = params.set(key, value);
    }
  }
  return params;
}
