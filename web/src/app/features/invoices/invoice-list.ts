import { CurrencyPipe, DatePipe } from '@angular/common';
import { httpResource } from '@angular/common/http';
import { Component, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { queryParams } from '../../core/api';
import {
  DocumentType,
  InvoiceStatus,
  InvoiceSummary,
  PagedResult,
  documentTypeLabels,
} from '../../core/models';
import { StatusBadge } from '../../shared/status-badge';

@Component({
  selector: 'app-invoice-list',
  imports: [RouterLink, FormsModule, CurrencyPipe, DatePipe, StatusBadge],
  template: `
    <header class="page-header">
      <h1>Documents</h1>
      <a class="btn btn-primary" routerLink="/documents/new">New document</a>
    </header>

    <div class="toolbar" role="search">
      <input
        type="search"
        placeholder="Search number, customer or NIF"
        aria-label="Search documents"
        [ngModel]="search()"
        (ngModelChange)="search.set($event); page.set(1)"
      />
      <select aria-label="Status" [ngModel]="status()" (ngModelChange)="status.set($event); page.set(1)">
        <option value="">All statuses</option>
        @for (s of statuses; track s) {
          <option [value]="s">{{ s }}</option>
        }
      </select>
      <select aria-label="Document type" [ngModel]="type()" (ngModelChange)="type.set($event); page.set(1)">
        <option value="">All types</option>
        @for (t of types; track t) {
          <option [value]="t">{{ typeLabels[t] }}</option>
        }
      </select>
    </div>

    @if (documents.error()) {
      <p class="alert">The documents could not be loaded. Check that the API is running.</p>
    }

    <div class="table-wrap">
      <table class="table">
        <thead>
          <tr>
            <th>Number</th>
            <th>Customer</th>
            <th>Date</th>
            <th>Due</th>
            <th>Status</th>
            <th class="num">Total</th>
          </tr>
        </thead>
        <tbody>
          @for (doc of documents.value()?.items; track doc.id) {
            <tr>
              <td>
                <a class="nowrap" [routerLink]="['/documents', doc.id]">{{ doc.documentNumber ?? 'Draft' }}</a>
              </td>
              <td>
                {{ doc.customerName }}
                <div class="muted small">{{ doc.customerNif }}</div>
              </td>
              <td>{{ doc.issueDate | date: 'dd/MM/yyyy' }}</td>
              <td>{{ doc.dueDate | date: 'dd/MM/yyyy' }}</td>
              <td><app-status-badge [status]="doc.status" /></td>
              <td class="num">{{ doc.grossTotal | currency }}</td>
            </tr>
          } @empty {
            <tr>
              <td colspan="6" class="empty">
                @if (documents.isLoading()) {
                  Loading documents…
                } @else {
                  No documents match these filters.
                  <a routerLink="/documents/new">Create a document</a>
                }
              </td>
            </tr>
          }
        </tbody>
      </table>
    </div>

    @if (totalPages() > 1) {
      <nav class="pager" aria-label="Pages">
        <button class="btn" [disabled]="page() === 1" (click)="page.set(page() - 1)">Previous</button>
        <span class="muted">Page {{ page() }} of {{ totalPages() }}</span>
        <button class="btn" [disabled]="page() >= totalPages()" (click)="page.set(page() + 1)">Next</button>
      </nav>
    }
  `,
  styles: `
    .small {
      font-size: var(--step--1);
    }
    .pager {
      display: flex;
      align-items: center;
      justify-content: flex-end;
      gap: 0.75rem;
      margin-top: 1rem;
    }
  `,
})
export class InvoiceList {
  protected readonly statuses: InvoiceStatus[] = ['Draft', 'Issued', 'Paid', 'Voided'];
  protected readonly types: DocumentType[] = ['Invoice', 'InvoiceReceipt', 'CreditNote'];
  protected readonly typeLabels = documentTypeLabels;

  protected readonly search = signal('');
  protected readonly status = signal<InvoiceStatus | ''>('');
  protected readonly type = signal<DocumentType | ''>('');
  protected readonly page = signal(1);

  protected readonly documents = httpResource<PagedResult<InvoiceSummary>>(() => ({
    url: '/api/invoices',
    params: queryParams({
      search: this.search(),
      status: this.status(),
      type: this.type(),
      page: this.page(),
      pageSize: 20,
    }),
  }));

  protected readonly totalPages = computed(() => this.documents.value()?.totalPages ?? 0);
}
