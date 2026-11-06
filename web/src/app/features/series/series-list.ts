import { DatePipe } from '@angular/common';
import { httpResource } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Api } from '../../core/api';
import { DocumentType, Series, SeriesVerification, documentTypeLabels } from '../../core/models';
import { problemMessage } from '../../core/problem';

/** Numbering series: open, close and verify the hash chain. Admins only. */
@Component({
  selector: 'app-series-list',
  imports: [FormsModule, DatePipe],
  template: `
    <header class="page-header">
      <h1>Numbering series</h1>
    </header>

    <p class="muted intro">
      Each document type numbers from one active series. Opening a series registers it and gets the
      validation code used in every ATCUD (simulated here; a certified program would call the AT
      service).
    </p>

    @if (error()) {
      <p class="alert" role="alert">{{ error() }}</p>
    }

    <form class="toolbar" (ngSubmit)="create()">
      <select name="type" aria-label="Document type" [(ngModel)]="newType">
        @for (t of types; track t) {
          <option [value]="t">{{ labels[t] }}</option>
        }
      </select>
      <input
        name="code"
        aria-label="Series code"
        placeholder="Code, e.g. 2027A"
        maxlength="20"
        [(ngModel)]="newCode"
      />
      <button class="btn btn-primary" type="submit" [disabled]="busy()">Open series</button>
    </form>

    <div class="table-wrap">
      <table class="table">
        <thead>
          <tr>
            <th>Series</th>
            <th>Validation code</th>
            <th class="num">Last number</th>
            <th>Last document</th>
            <th>Hash chain</th>
            <th><span class="sr-only">Actions</span></th>
          </tr>
        </thead>
        <tbody>
          @for (s of series.value(); track s.id) {
            <tr [class.closed]="!s.isActive">
              <td>
                <strong>{{ code(s.documentType) }} {{ s.code }}</strong>
                <div class="muted small">{{ s.isActive ? 'Active' : 'Closed' }}</div>
              </td>
              <td class="mono">{{ s.validationCode }}</td>
              <td class="num">{{ s.lastNumber }}</td>
              <td>{{ (s.lastIssueDate | date: 'dd/MM/yyyy') ?? '—' }}</td>
              <td>
                @if (results()[s.id]; as r) {
                  @if (r.isValid) {
                    <span class="ok">Intact ({{ r.documentsChecked }} documents)</span>
                  } @else {
                    <span class="broken">Broken at no. {{ r.firstBrokenNumber }}</span>
                  }
                } @else {
                  <button class="btn-link" type="button" (click)="verify(s)">Verify</button>
                }
              </td>
              <td class="actions">
                @if (s.isActive) {
                  <button class="btn-link danger" type="button" (click)="close(s)">Close</button>
                }
              </td>
            </tr>
          }
        </tbody>
      </table>
    </div>
  `,
  styles: `
    .intro {
      max-width: 65ch;
      margin: -0.75rem 0 1.25rem;
    }
    .small {
      font-size: var(--step--1);
    }
    .mono {
      letter-spacing: 0.06em;
      font-weight: 500;
    }
    .closed td {
      color: var(--muted);
    }
    .ok {
      color: var(--paid);
      font-weight: 600;
    }
    .broken {
      color: var(--stamp);
      font-weight: 700;
    }
    .actions {
      text-align: right;
    }
    .btn-link {
      cursor: pointer;
    }
    .danger {
      color: var(--stamp);
    }
    .sr-only {
      position: absolute;
      width: 1px;
      height: 1px;
      overflow: hidden;
      clip: rect(0 0 0 0);
    }
  `,
})
export class SeriesList {
  private readonly api = inject(Api);

  protected readonly series = httpResource<Series[]>(() => '/api/series');
  protected readonly types: DocumentType[] = ['Invoice', 'InvoiceReceipt', 'CreditNote'];
  protected readonly labels = documentTypeLabels;
  protected readonly results = signal<Record<string, SeriesVerification>>({});
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);
  protected newType: DocumentType = 'Invoice';
  protected newCode = '';

  protected code(type: DocumentType): string {
    return { Invoice: 'FT', InvoiceReceipt: 'FR', CreditNote: 'NC' }[type];
  }

  protected create(): void {
    this.busy.set(true);
    this.error.set(null);
    this.api.createSeries(this.newType, this.newCode.trim()).subscribe({
      next: () => {
        this.busy.set(false);
        this.newCode = '';
        this.series.reload();
      },
      error: (e: unknown) => {
        this.busy.set(false);
        this.error.set(problemMessage(e));
      },
    });
  }

  protected close(series: Series): void {
    if (
      confirm(
        `Close ${this.code(series.documentType)} ${series.code}? No more documents can be issued in it.`,
      )
    ) {
      this.api.closeSeries(series.id).subscribe({
        next: () => this.series.reload(),
        error: (e: unknown) => this.error.set(problemMessage(e)),
      });
    }
  }

  protected verify(series: Series): void {
    this.api.verifySeries(series.id).subscribe({
      next: (result) => this.results.update((all) => ({ ...all, [series.id]: result })),
      error: (e: unknown) => this.error.set(problemMessage(e)),
    });
  }
}
