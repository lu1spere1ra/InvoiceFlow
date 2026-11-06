import { httpResource } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Auth } from '../../core/auth';
import { queryParams } from '../../core/api';
import { Customer, PagedResult } from '../../core/models';

@Component({
  selector: 'app-customer-list',
  imports: [RouterLink, FormsModule],
  template: `
    <header class="page-header">
      <h1>Customers</h1>
      @if (auth.canWrite()) {
        <a class="btn btn-primary" routerLink="/customers/new">New customer</a>
      }
    </header>

    <div class="toolbar" role="search">
      <input
        type="search"
        placeholder="Search by name or exact NIF"
        aria-label="Search customers"
        [ngModel]="search()"
        (ngModelChange)="search.set($event)"
      />
    </div>

    <div class="table-wrap">
      <table class="table">
        <thead>
          <tr>
            <th>Name</th>
            <th>NIF</th>
            <th>City</th>
            <th>Email</th>
          </tr>
        </thead>
        <tbody>
          @for (customer of customers.value()?.items; track customer.id) {
            <tr>
              <td>{{ customer.name }}</td>
              <td class="tabular">{{ customer.nif }}</td>
              <td>{{ customer.city }}</td>
              <td>{{ customer.email ?? '—' }}</td>
            </tr>
          } @empty {
            <tr>
              <td colspan="4" class="empty">
                {{ customers.isLoading() ? 'Loading customers…' : 'No customers found.' }}
              </td>
            </tr>
          }
        </tbody>
      </table>
    </div>
  `,
  styles: `
    .tabular {
      font-variant-numeric: tabular-nums;
    }
  `,
})
export class CustomerList {
  protected readonly auth = inject(Auth);
  protected readonly search = signal('');

  protected readonly customers = httpResource<PagedResult<Customer>>(() => ({
    url: '/api/customers',
    params: queryParams({ search: this.search(), pageSize: 100 }),
  }));
}
