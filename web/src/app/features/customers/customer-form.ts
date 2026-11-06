import { httpResource } from '@angular/common/http';
import { Component, effect, inject, input, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Api } from '../../core/api';
import { Customer } from '../../core/models';
import { problemMessage } from '../../core/problem';

@Component({
  selector: 'app-customer-form',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <header class="page-header">
      <h1>{{ id() ? 'Edit customer' : 'New customer' }}</h1>
      @if (id()) {
        <button class="btn btn-danger" type="button" [disabled]="saving()" (click)="remove()">
          Delete customer
        </button>
      }
    </header>

    @if (error()) {
      <p class="alert" role="alert">{{ error() }}</p>
    }

    <form class="form" [formGroup]="form" (ngSubmit)="save()">
      <div class="field wide">
        <label for="name">Name</label>
        <input id="name" formControlName="name" autocomplete="organization" />
      </div>
      <div class="field">
        <label for="nif">NIF</label>
        <input id="nif" formControlName="nif" inputmode="numeric" maxlength="9" />
        @if (form.controls.nif.touched && form.controls.nif.invalid) {
          <span class="field-error">Enter the 9-digit tax number.</span>
        }
      </div>
      <div class="field">
        <label for="email">Email (optional)</label>
        <input id="email" type="email" formControlName="email" />
      </div>
      <div class="field wide">
        <label for="street">Address</label>
        <input id="street" formControlName="street" autocomplete="street-address" />
      </div>
      <div class="field">
        <label for="postalCode">Postal code</label>
        <input id="postalCode" formControlName="postalCode" placeholder="0000-000" />
      </div>
      <div class="field">
        <label for="city">City</label>
        <input id="city" formControlName="city" />
      </div>
      <div class="actions wide">
        <button class="btn btn-primary" type="submit" [disabled]="saving()">Save customer</button>
        <a class="btn" routerLink="/customers">Cancel</a>
      </div>
    </form>
  `,
  styles: `
    .form {
      display: grid;
      grid-template-columns: repeat(2, minmax(0, 1fr));
      gap: 1rem 1.25rem;
      max-width: 40rem;
    }
    .wide {
      grid-column: 1 / -1;
    }
    .actions {
      display: flex;
      gap: 0.6rem;
      margin-top: 0.5rem;
    }
    @media (max-width: 560px) {
      .form {
        grid-template-columns: 1fr;
      }
    }
  `,
})
export class CustomerForm {
  private readonly api = inject(Api);
  private readonly router = inject(Router);

  /** Route parameter; absent when creating. */
  readonly id = input<string>();
  private readonly existing = httpResource<Customer>(() =>
    this.id() ? `/api/customers/${this.id()}` : undefined,
  );

  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly form = inject(NonNullableFormBuilder).group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    nif: ['', [Validators.required, Validators.pattern(/^\d{9}$/)]],
    email: [''],
    street: ['', Validators.required],
    postalCode: ['', Validators.required],
    city: ['', Validators.required],
  });

  constructor() {
    effect(() => {
      const customer = this.existing.value();
      if (customer) {
        this.form.patchValue({ ...customer, email: customer.email ?? '' });
      }
    });
  }

  protected remove(): void {
    const id = this.id();
    if (
      id &&
      confirm('Delete this customer? Issued documents keep their copy of the customer data.')
    ) {
      this.api.deleteCustomer(id).subscribe({
        next: () => this.router.navigate(['/customers']),
        error: (e: unknown) => this.error.set(problemMessage(e)),
      });
    }
  }

  protected save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.error.set(null);
    const value = this.form.getRawValue();
    const input = { ...value, email: value.email || null, country: 'PT' };
    const id = this.id();
    (id ? this.api.updateCustomer(id, input) : this.api.createCustomer(input)).subscribe({
      next: () => this.router.navigate(['/customers']),
      error: (e: unknown) => {
        this.error.set(problemMessage(e));
        this.saving.set(false);
      },
    });
  }
}
