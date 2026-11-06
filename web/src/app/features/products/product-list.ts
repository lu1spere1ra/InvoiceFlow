import { CurrencyPipe } from '@angular/common';
import { httpResource } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Api } from '../../core/api';
import { Auth } from '../../core/auth';
import { Product, ProductType, TaxRate } from '../../core/models';
import { problemMessage } from '../../core/problem';

/** Catalog of articles and services, edited inline: SMEs typically have a few dozen items. */
@Component({
  selector: 'app-product-list',
  imports: [ReactiveFormsModule, CurrencyPipe],
  template: `
    <header class="page-header">
      <h1>Products and services</h1>
      @if (auth.canWrite() && !editing()) {
        <button class="btn btn-primary" (click)="startCreate()">New product</button>
      }
    </header>

    @if (error()) {
      <p class="alert" role="alert">{{ error() }}</p>
    }

    @if (editing()) {
      <form class="editor" [formGroup]="form" (ngSubmit)="save()">
        <h2>{{ editingId() ? 'Edit ' + form.controls.code.value : 'New product' }}</h2>
        <div class="grid">
          <div class="field">
            <label for="code">Code</label>
            <input id="code" formControlName="code" />
          </div>
          <div class="field wide">
            <label for="description">Description</label>
            <input id="description" formControlName="description" />
          </div>
          <div class="field">
            <label for="type">Type</label>
            <select id="type" formControlName="type">
              <option value="Service">Service</option>
              <option value="Goods">Goods</option>
            </select>
          </div>
          <div class="field">
            <label for="unit">Unit</label>
            <input id="unit" formControlName="unit" placeholder="UN, H, KG…" />
          </div>
          <div class="field">
            <label for="price">Unit price (€, before VAT)</label>
            <input id="price" type="number" step="0.01" min="0" formControlName="unitPrice" />
          </div>
          <div class="field">
            <label for="rate">VAT rate</label>
            <select id="rate" formControlName="taxRateId">
              @for (r of taxRates.value(); track r.id) {
                <option [value]="r.id">{{ r.description }} ({{ r.percentage }}%)</option>
              }
            </select>
          </div>
        </div>
        <div class="actions">
          <button class="btn btn-primary" type="submit" [disabled]="busy()">Save product</button>
          <button class="btn" type="button" (click)="editing.set(false)">Cancel</button>
        </div>
      </form>
    }

    <div class="table-wrap">
      <table class="table">
        <thead>
          <tr>
            <th>Code</th>
            <th>Description</th>
            <th>Unit</th>
            <th>VAT</th>
            <th class="num">Price</th>
            @if (auth.canWrite()) {
              <th><span class="visually-hidden">Actions</span></th>
            }
          </tr>
        </thead>
        <tbody>
          @for (p of products.value(); track p.id) {
            <tr>
              <td class="nowrap">{{ p.code }}</td>
              <td>
                {{ p.description }}
                <div class="muted small">{{ p.type === 'Service' ? 'Service' : 'Goods' }}</div>
              </td>
              <td>{{ p.unit }}</td>
              <td>{{ rateLabel(p.taxRateId) }}</td>
              <td class="num">{{ p.unitPrice | currency: 'EUR' : 'symbol' : '1.2-4' }}</td>
              @if (auth.canWrite()) {
                <td class="row-actions">
                  <button class="btn-link" (click)="startEdit(p)">Edit</button>
                  <button class="btn-link danger" (click)="remove(p)">Delete</button>
                </td>
              }
            </tr>
          } @empty {
            <tr>
              <td colspan="6" class="empty">
                {{ products.isLoading() ? 'Loading…' : 'No products yet.' }}
              </td>
            </tr>
          }
        </tbody>
      </table>
    </div>
  `,
  styles: `
    .editor {
      display: grid;
      gap: 1rem;
      margin-bottom: 1.5rem;
      padding: 1.25rem;
      background: var(--sheet);
      border: 1px solid var(--rule);
      border-radius: var(--radius);
    }
    .grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(12rem, 1fr));
      gap: 1rem;
    }
    .wide {
      grid-column: span 2;
    }
    .actions,
    .row-actions {
      display: flex;
      gap: 0.5rem;
      justify-content: flex-end;
    }
    .actions {
      justify-content: flex-start;
    }
    .btn-link {
      cursor: pointer;
    }
    .danger {
      color: var(--stamp);
    }
    .small {
      font-size: var(--step--1);
    }
    .visually-hidden {
      position: absolute;
      clip: rect(0 0 0 0);
      width: 1px;
      height: 1px;
      overflow: hidden;
    }
    @media (max-width: 560px) {
      .wide {
        grid-column: auto;
      }
    }
  `,
})
export class ProductList {
  private readonly api = inject(Api);
  protected readonly auth = inject(Auth);

  protected readonly products = httpResource<Product[]>(() => '/api/products');
  protected readonly taxRates = httpResource<TaxRate[]>(() => '/api/tax-rates');
  private readonly ratesById = computed(
    () => new Map((this.taxRates.value() ?? []).map((r) => [r.id, r])),
  );

  protected readonly editing = signal(false);
  protected readonly editingId = signal<string | null>(null);
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly form = inject(NonNullableFormBuilder).group({
    code: ['', [Validators.required, Validators.maxLength(60)]],
    description: ['', [Validators.required, Validators.maxLength(200)]],
    type: ['Service' as ProductType, Validators.required],
    unit: ['UN', Validators.required],
    unitPrice: [0, [Validators.required, Validators.min(0)]],
    taxRateId: ['', Validators.required],
  });

  protected rateLabel(id: string): string {
    const rate = this.ratesById().get(id);
    return rate ? `${rate.percentage}%` : '';
  }

  protected startCreate(): void {
    const normal = this.taxRates.value()?.find((r) => r.code === 'Normal');
    this.form.reset({
      code: '',
      description: '',
      type: 'Service',
      unit: 'UN',
      unitPrice: 0,
      taxRateId: normal?.id ?? '',
    });
    this.form.controls.code.enable();
    this.editingId.set(null);
    this.editing.set(true);
  }

  protected startEdit(product: Product): void {
    this.form.reset({ ...product });
    this.form.controls.code.disable(); // the code identifies the product in SAF-T
    this.editingId.set(product.id);
    this.editing.set(true);
  }

  protected save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const id = this.editingId();
    this.busy.set(true);
    this.error.set(null);
    (id ? this.api.updateProduct(id, value) : this.api.createProduct(value)).subscribe({
      next: () => {
        this.busy.set(false);
        this.editing.set(false);
        this.products.reload();
      },
      error: (e: unknown) => {
        this.busy.set(false);
        this.error.set(problemMessage(e));
      },
    });
  }

  protected remove(product: Product): void {
    if (!confirm(`Delete ${product.code}? Existing documents keep their copy of it.`)) {
      return;
    }
    this.api.deleteProduct(product.id).subscribe({
      next: () => this.products.reload(),
      error: (e: unknown) => this.error.set(problemMessage(e)),
    });
  }
}
