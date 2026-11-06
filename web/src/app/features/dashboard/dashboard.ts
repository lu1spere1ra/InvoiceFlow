import { CurrencyPipe } from '@angular/common';
import { httpResource } from '@angular/common/http';
import { Component, computed } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MonthlyRevenue, RevenueChart } from './revenue-chart';

interface Dashboard {
  revenueThisMonth: number;
  revenueYearToDate: number;
  outstandingTotal: number;
  overdueTotal: number;
  revenueByMonth: MonthlyRevenue[];
  topCustomers: { nif: string; name: string; netRevenue: number; documentCount: number }[];
  outstanding: {
    label: string;
    minDaysOverdue: number;
    maxDaysOverdue: number | null;
    amount: number;
    invoiceCount: number;
  }[];
}

@Component({
  selector: 'app-dashboard',
  imports: [CurrencyPipe, RouterLink, RevenueChart],
  template: `
    <header class="page-header">
      <h1>Overview</h1>
    </header>

    @if (dashboard.error()) {
      <p class="alert">The overview could not be loaded. Check that the API is running.</p>
    }

    @if (dashboard.value(); as d) {
      <section class="tiles" aria-label="Key figures">
        <div class="tile">
          <p class="label">Revenue this month</p>
          <p class="value">{{ d.revenueThisMonth | currency }}</p>
          <p class="hint">Net of VAT and credit notes</p>
        </div>
        <div class="tile">
          <p class="label">Year to date</p>
          <p class="value">{{ d.revenueYearToDate | currency }}</p>
          <p class="hint">Net of VAT and credit notes</p>
        </div>
        <div class="tile">
          <p class="label">Waiting for payment</p>
          <p class="value">{{ d.outstandingTotal | currency }}</p>
          <p class="hint">Issued invoices not yet paid</p>
        </div>
        <a class="tile tile-link" routerLink="/documents" [queryParams]="{ overdue: 1 }">
          <p class="label">Overdue</p>
          <p class="value">{{ d.overdueTotal | currency }}</p>
          <p class="hint" [class.alarm]="d.overdueTotal > 0">
            {{ d.overdueTotal > 0 ? '⚠ Past the due date' : 'Nothing overdue' }}
          </p>
        </a>
      </section>

      <section class="card">
        <h2>Net revenue per month</h2>
        <app-revenue-chart [data]="d.revenueByMonth" />
      </section>

      <div class="columns">
        <section class="card">
          <h2>Top customers <span class="muted">· last 12 months</span></h2>
          @if (d.topCustomers.length === 0) {
            <p class="muted">No issued documents yet.</p>
          }
          <ol class="ranking">
            @for (c of d.topCustomers; track c.nif) {
              <li>
                <div class="row">
                  <span class="name">{{ c.name }}</span>
                  <span class="num">{{ c.netRevenue | currency }}</span>
                </div>
                <div class="track" aria-hidden="true">
                  <div class="fill" [style.width.%]="share(c.netRevenue)"></div>
                </div>
                <span class="muted small">{{ c.documentCount }} documents · NIF {{ c.nif }}</span>
              </li>
            }
          </ol>
        </section>

        <section class="card">
          <h2>Waiting for payment, by days overdue</h2>
          <table class="aging">
            <thead>
              <tr>
                <th>Overdue</th>
                <th class="num">Invoices</th>
                <th class="num">Amount</th>
              </tr>
            </thead>
            <tbody>
              @for (b of d.outstanding; track b.label) {
                <tr>
                  <td>
                    {{ b.label }}
                    <div class="track" aria-hidden="true">
                      <div
                        class="fill"
                        [class.overdue]="b.minDaysOverdue > 0"
                        [style.width.%]="agingShare(b.amount)"
                      ></div>
                    </div>
                  </td>
                  <td class="num">{{ b.invoiceCount }}</td>
                  <td class="num">{{ b.amount | currency }}</td>
                </tr>
              }
            </tbody>
          </table>
          <a class="more" routerLink="/documents">See documents</a>
        </section>
      </div>
    } @else if (dashboard.isLoading()) {
      <p class="muted">Loading overview…</p>
    }
  `,
  styles: `
    .tiles {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(12rem, 1fr));
      gap: 1rem;
      margin-bottom: 1.5rem;
    }
    .tile {
      padding: 1rem 1.25rem;
      background: var(--sheet);
      border: 1px solid var(--rule);
      border-radius: var(--radius);
      color: inherit;
      text-decoration: none;
    }
    .tile p {
      margin: 0;
    }
    .tile-link:hover {
      border-color: var(--rule-strong);
    }
    .label {
      color: var(--muted);
      font-weight: 500;
    }
    .value {
      margin: 0.2rem 0 !important;
      font-size: var(--step-2);
      font-weight: 700;
      letter-spacing: -0.01em;
      font-variant-numeric: tabular-nums;
    }
    .hint {
      font-size: var(--step--1);
      color: var(--muted);
    }
    .hint.alarm {
      color: var(--stamp);
      font-weight: 600;
    }
    .card {
      padding: 1.25rem;
      margin-bottom: 1.5rem;
      background: var(--sheet);
      border: 1px solid var(--rule);
      border-radius: var(--radius);
    }
    .card h2 {
      margin-bottom: 1rem;
    }
    .card h2 .muted {
      font-weight: 400;
    }
    .columns {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(20rem, 1fr));
      gap: 1.5rem;
    }
    .columns .card {
      margin-bottom: 0;
    }
    .ranking {
      display: grid;
      gap: 0.9rem;
      margin: 0;
      padding: 0;
      list-style: none;
    }
    .row {
      display: flex;
      justify-content: space-between;
      gap: 1rem;
    }
    .name {
      font-weight: 500;
    }
    .track {
      height: 6px;
      margin: 0.3rem 0 0.2rem;
      background: var(--paper);
      border-radius: 3px;
    }
    .fill {
      height: 100%;
      min-width: 2px;
      background: var(--chart);
      border-radius: 3px;
    }
    .fill.overdue {
      background: var(--ink);
    }
    .small {
      font-size: var(--step--1);
    }
    .aging {
      width: 100%;
      border-collapse: collapse;
    }
    .aging th {
      padding-bottom: 0.5rem;
      font-size: var(--step--1);
      font-weight: 500;
      color: var(--muted);
      text-align: left;
    }
    .aging th.num {
      text-align: right;
    }
    .aging td {
      padding: 0.45rem 0;
      border-bottom: 1px solid var(--rule);
      vertical-align: top;
    }
    .aging td:first-child {
      width: 55%;
      padding-right: 1rem;
    }
    .more {
      display: inline-block;
      margin-top: 0.75rem;
      font-size: var(--step--1);
    }
  `,
})
export class DashboardPage {
  protected readonly dashboard = httpResource<Dashboard>(() => '/api/dashboard');

  private readonly topMax = computed(() =>
    Math.max(1, ...(this.dashboard.value()?.topCustomers ?? []).map((c) => c.netRevenue)),
  );
  private readonly agingMax = computed(() =>
    Math.max(1, ...(this.dashboard.value()?.outstanding ?? []).map((b) => b.amount)),
  );

  protected share(value: number): number {
    return Math.max(0, (value / this.topMax()) * 100);
  }

  protected agingShare(value: number): number {
    return Math.max(0, (value / this.agingMax()) * 100);
  }
}
