import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { queryParams } from '../../core/api';
import { problemMessage } from '../../core/problem';

/** Downloads the SAF-T (PT) file for a period, typically a month for the monthly AT submission. */
@Component({
  selector: 'app-saft-export',
  imports: [FormsModule],
  template: `
    <header class="page-header">
      <h1>SAF-T (PT) export</h1>
    </header>

    <p class="muted intro">
      Generates the SAF-T (PT) 1.04_01 file with every document issued in the period, validated
      against the official schema. Periods must stay within one fiscal year.
    </p>

    @if (error()) {
      <p class="alert" role="alert">{{ error() }}</p>
    }

    <form class="toolbar" (ngSubmit)="download()">
      <div class="field">
        <label for="from">From</label>
        <input id="from" name="from" type="date" [(ngModel)]="from" required />
      </div>
      <div class="field">
        <label for="to">To</label>
        <input id="to" name="to" type="date" [(ngModel)]="to" required />
      </div>
      <button class="btn btn-primary" type="submit" [disabled]="busy()">Download SAF-T file</button>
    </form>
    <p class="muted small">
      InvoiceFlow is not certified: the file is for demonstration and must not be submitted to AT.
    </p>
  `,
  styles: `
    .intro {
      max-width: 65ch;
      margin: -0.75rem 0 1.25rem;
    }
    .toolbar {
      align-items: flex-end;
    }
    .small {
      font-size: var(--step--1);
    }
  `,
})
export class SaftExport {
  private readonly http = inject(HttpClient);

  protected from = firstDayOfPreviousMonth();
  protected to = lastDayOfPreviousMonth();
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);

  protected download(): void {
    this.busy.set(true);
    this.error.set(null);
    this.http
      .get('/api/saft', {
        params: queryParams({ from: this.from, to: this.to }),
        responseType: 'blob',
      })
      .subscribe({
        next: (blob) => {
          const url = URL.createObjectURL(blob);
          const link = document.createElement('a');
          link.href = url;
          link.download = `SAFT-PT_${this.from.replaceAll('-', '')}_${this.to.replaceAll('-', '')}.xml`;
          link.click();
          URL.revokeObjectURL(url);
          this.busy.set(false);
        },
        error: async (e: unknown) => {
          this.busy.set(false);
          this.error.set(problemMessage(await blobErrorToJson(e)));
        },
      });
  }
}

const iso = (d: Date) =>
  `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;

function firstDayOfPreviousMonth(): string {
  const now = new Date();
  return iso(new Date(now.getFullYear(), now.getMonth() - 1, 1));
}

function lastDayOfPreviousMonth(): string {
  const now = new Date();
  return iso(new Date(now.getFullYear(), now.getMonth(), 0));
}

/** Errors of blob requests arrive as a Blob; parse them back into ProblemDetails. */
async function blobErrorToJson(error: unknown): Promise<unknown> {
  if (error instanceof HttpErrorResponse && error.error instanceof Blob) {
    try {
      return new HttpErrorResponse({
        error: JSON.parse(await error.error.text()),
        status: error.status,
      });
    } catch {
      return error;
    }
  }
  return error;
}
