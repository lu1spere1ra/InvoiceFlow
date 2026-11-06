import { CurrencyPipe, DatePipe, PercentPipe } from '@angular/common';
import { httpResource } from '@angular/common/http';
import { Component, inject, input, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { Api } from '../../core/api';
import { Invoice, documentTypeLabels } from '../../core/models';
import { problemMessage } from '../../core/problem';
import { StatusBadge } from '../../shared/status-badge';

@Component({
  selector: 'app-invoice-detail',
  imports: [RouterLink, CurrencyPipe, DatePipe, PercentPipe, StatusBadge],
  templateUrl: './invoice-detail.html',
  styleUrl: './invoice-detail.css',
})
export class InvoiceDetail {
  private readonly api = inject(Api);
  private readonly router = inject(Router);

  readonly id = input.required<string>();

  protected readonly invoice = httpResource<Invoice>(() => `/api/invoices/${this.id()}`);
  protected readonly typeLabels = documentTypeLabels;
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);

  protected issue(): void {
    this.run(this.api.issue(this.id()), () => this.invoice.reload());
  }

  protected deleteDraft(): void {
    if (confirm('Delete this draft? This cannot be undone.')) {
      this.run(this.api.deleteDraft(this.id()), () => this.router.navigate(['/documents']));
    }
  }

  private run<T>(request: Observable<T>, done: () => void): void {
    this.busy.set(true);
    this.error.set(null);
    request.subscribe({
      next: () => {
        this.busy.set(false);
        done();
      },
      error: (e: unknown) => {
        this.busy.set(false);
        this.error.set(problemMessage(e));
      },
    });
  }
}
