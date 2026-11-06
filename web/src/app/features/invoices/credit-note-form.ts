import { CurrencyPipe } from '@angular/common';
import { httpResource } from '@angular/common/http';
import { Component, computed, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Api } from '../../core/api';
import { Invoice } from '../../core/models';
import { problemMessage } from '../../core/problem';
import { previewTotals } from '../../shared/totals';

/** Picks which lines of an issued invoice to credit, and how much of each. */
@Component({
  selector: 'app-credit-note-form',
  imports: [FormsModule, RouterLink, CurrencyPipe],
  template: `
    @if (original.value(); as doc) {
      <header class="page-header">
        <div>
          <a [routerLink]="['/documents', doc.id]" class="back">{{ doc.documentNumber }}</a>
          <h1>New credit note</h1>
        </div>
      </header>

      @if (error()) {
        <p class="alert" role="alert">{{ error() }}</p>
      }

      <p class="muted">
        Still creditable on {{ doc.documentNumber }}:
        <strong>{{ doc.grossTotal - doc.creditedAmount | currency }}</strong>
        of {{ doc.grossTotal | currency }}.
      </p>

      <div class="table-wrap">
        <table class="table">
          <thead>
            <tr>
              <th>Line</th>
              <th class="num">Invoiced</th>
              <th class="num">Unit price</th>
              <th class="num">Quantity to credit</th>
            </tr>
          </thead>
          <tbody>
            @for (line of doc.lines; track line.lineNumber; let i = $index) {
              <tr>
                <td>
                  {{ line.description }} <span class="muted">· {{ line.taxPercentage }}% VAT</span>
                </td>
                <td class="num">{{ line.quantity }} {{ line.unit }}</td>
                <td class="num">{{ line.unitPrice | currency }}</td>
                <td class="num qty">
                  <input
                    type="number"
                    min="0"
                    [max]="line.quantity"
                    step="any"
                    [attr.aria-label]="'Quantity to credit, line ' + line.lineNumber"
                    [ngModel]="quantities()[i] ?? 0"
                    (ngModelChange)="setQuantity(i, $event)"
                  />
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>

      <div class="footer">
        <div class="field reason">
          <label for="reason">Reason (printed on the credit note)</label>
          <input
            id="reason"
            [(ngModel)]="reason"
            maxlength="200"
            placeholder="e.g. Returned goods, pricing error"
          />
        </div>
        <p class="total">
          Credit note total <strong>{{ total() | currency }}</strong>
        </p>
        <button class="btn btn-primary" [disabled]="busy()" (click)="create(doc)">
          Create draft credit note
        </button>
      </div>
    } @else {
      <p class="muted">Loading invoice…</p>
    }
  `,
  styles: `
    .back {
      font-size: var(--step--1);
      text-decoration: none;
    }
    .qty input {
      max-width: 7rem;
    }
    .footer {
      display: grid;
      justify-items: end;
      gap: 1rem;
      margin-top: 1.5rem;
    }
    .reason {
      width: min(100%, 32rem);
    }
    .total {
      margin: 0;
      font-size: var(--step-1);
    }
  `,
})
export class CreditNoteForm {
  private readonly api = inject(Api);
  private readonly router = inject(Router);

  readonly id = input.required<string>();
  protected readonly original = httpResource<Invoice>(() => `/api/invoices/${this.id()}`);
  protected readonly quantities = signal<number[]>([]);
  protected reason = '';
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly total = computed(() => {
    const lines = this.original.value()?.lines ?? [];
    return previewTotals(
      lines.map((line, i) => ({
        quantity: this.quantities()[i] ?? 0,
        unitPrice: line.unitPrice,
        discountPercent: line.discountPercent,
        taxPercentage: line.taxPercentage,
      })),
    ).grossTotal;
  });

  protected setQuantity(index: number, value: number): void {
    const next = [...this.quantities()];
    next[index] = Number(value) || 0;
    this.quantities.set(next);
  }

  protected create(doc: Invoice): void {
    const lines = doc.lines
      .map((line, i) => ({ lineNumber: line.lineNumber, quantity: this.quantities()[i] ?? 0 }))
      .filter((line) => line.quantity > 0);

    if (lines.length === 0 || !this.reason.trim()) {
      this.error.set('Enter a quantity on at least one line and the reason for the credit note.');
      return;
    }

    this.busy.set(true);
    this.error.set(null);
    this.api.createCreditNote(doc.id, { reason: this.reason, lines }).subscribe({
      next: (note) => this.router.navigate(['/documents', note.id]),
      error: (e: unknown) => {
        this.busy.set(false);
        this.error.set(problemMessage(e));
      },
    });
  }
}
