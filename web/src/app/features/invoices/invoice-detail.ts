import { CurrencyPipe, DatePipe, PercentPipe } from '@angular/common';
import { httpResource } from '@angular/common/http';
import { Component, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { Api } from '../../core/api';
import { Auth } from '../../core/auth';
import { Invoice, documentTypeLabels } from '../../core/models';
import { problemMessage } from '../../core/problem';
import { StatusBadge } from '../../shared/status-badge';

@Component({
  selector: 'app-invoice-detail',
  imports: [RouterLink, FormsModule, CurrencyPipe, DatePipe, PercentPipe, StatusBadge],
  templateUrl: './invoice-detail.html',
  styleUrl: './invoice-detail.css',
})
export class InvoiceDetail {
  private readonly api = inject(Api);
  private readonly router = inject(Router);
  protected readonly auth = inject(Auth);

  readonly id = input.required<string>();

  protected readonly invoice = httpResource<Invoice>(() => `/api/invoices/${this.id()}`);
  protected readonly typeLabels = documentTypeLabels;
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);

  protected issue(): void {
    this.run(this.api.issue(this.id()), () => this.invoice.reload());
  }

  protected downloadPdf(doc: Invoice): void {
    this.api.pdf(doc.id).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `${(doc.documentNumber ?? 'draft').replaceAll(' ', '_').replaceAll('/', '-')}.pdf`;
        link.click();
        URL.revokeObjectURL(url);
      },
      error: (e: unknown) => this.error.set(problemMessage(e)),
    });
  }

  /** Which inline confirmation panel is open. */
  protected readonly pending = signal<'pay' | 'void' | null>(null);
  protected readonly today = new Date().toISOString().slice(0, 10);
  protected paidOn = this.today;
  protected voidReason = '';

  protected confirmPayment(): void {
    this.run(this.api.markPaid(this.id(), this.paidOn), () => this.done());
  }

  protected confirmVoid(): void {
    if (!this.voidReason.trim()) {
      this.error.set('State why the document is being voided.');
      return;
    }
    this.run(this.api.void(this.id(), this.voidReason.trim()), () => this.done());
  }

  private done(): void {
    this.pending.set(null);
    this.invoice.reload();
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
