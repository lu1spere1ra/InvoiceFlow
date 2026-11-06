import { CurrencyPipe } from '@angular/common';
import { httpResource } from '@angular/common/http';
import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { Api } from '../../core/api';
import {
  Customer,
  ExemptionReason,
  Invoice,
  InvoiceLineInput,
  PagedResult,
  Product,
  TaxRate,
} from '../../core/models';
import { problemMessage } from '../../core/problem';
import { previewTotals } from '../../shared/totals';

@Component({
  selector: 'app-invoice-editor',
  imports: [ReactiveFormsModule, RouterLink, CurrencyPipe],
  templateUrl: './invoice-editor.html',
  styleUrl: './invoice-editor.css',
})
export class InvoiceEditor {
  private readonly api = inject(Api);
  private readonly router = inject(Router);
  private readonly fb = inject(NonNullableFormBuilder);

  /** Route parameter; absent when creating a new draft. */
  readonly id = input<string>();

  protected readonly customers = httpResource<PagedResult<Customer>>(
    () => '/api/customers?pageSize=100',
  );
  protected readonly products = httpResource<Product[]>(() => '/api/products');
  protected readonly taxRates = httpResource<TaxRate[]>(() => '/api/tax-rates');
  protected readonly exemptionReasons = httpResource<ExemptionReason[]>(
    () => '/api/tax-rates/exemption-reasons',
  );
  protected readonly existing = httpResource<Invoice>(() =>
    this.id() ? `/api/invoices/${this.id()}` : undefined,
  );

  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly form = this.fb.group({
    customerId: ['', Validators.required],
    paymentTermDays: [30, [Validators.required, Validators.min(0), Validators.max(365)]],
    lines: this.fb.array([this.newLine()]),
  });

  private readonly formValue = toSignal(this.form.valueChanges, {
    initialValue: this.form.getRawValue(),
  });

  protected readonly totals = computed(() => {
    const rates = new Map((this.taxRates.value() ?? []).map((r) => [r.id, r.percentage]));
    return previewTotals(
      (this.formValue().lines ?? []).map((l) => ({
        quantity: Number(l.quantity) || 0,
        unitPrice: Number(l.unitPrice) || 0,
        discountPercent: Number(l.discountPercent) || 0,
        taxPercentage: rates.get(l.taxRateId ?? '') ?? 0,
      })),
    );
  });

  constructor() {
    // Fill the form once when an existing draft has loaded.
    effect(() => {
      const invoice = this.existing.value();
      if (!invoice) {
        return;
      }
      const rates = this.taxRates.value() ?? [];
      this.form.controls.lines.clear();
      for (const line of invoice.lines) {
        const rate = rates.find(
          (r) => r.code === line.taxCode && r.percentage === line.taxPercentage,
        );
        this.form.controls.lines.push(
          this.newLine({
            productId: line.productId,
            description: line.description,
            quantity: line.quantity,
            unitPrice: line.unitPrice,
            discountPercent: line.discountPercent,
            taxRateId: rate?.id ?? '',
            exemptionCode: line.exemptionCode ?? '',
          }),
        );
      }
      this.form.patchValue({
        customerId: invoice.customerId,
        paymentTermDays: invoice.paymentTermDays,
      });
    });
  }

  protected get lines() {
    return this.form.controls.lines;
  }

  protected isExempt(index: number): boolean {
    const rateId = this.lines.at(index).controls.taxRateId.value;
    return this.taxRates.value()?.find((r) => r.id === rateId)?.code === 'Exempt';
  }

  protected addLine(): void {
    this.lines.push(this.newLine());
  }

  protected removeLine(index: number): void {
    this.lines.removeAt(index);
  }

  /** Picking a product pre-fills price, VAT rate and description; the user can still change them. */
  protected productChanged(index: number): void {
    const line = this.lines.at(index);
    const product = this.products.value()?.find((p) => p.id === line.controls.productId.value);
    if (product) {
      line.patchValue({
        unitPrice: product.unitPrice,
        taxRateId: product.taxRateId,
        description: product.description,
        exemptionCode: '',
      });
    }
  }

  protected save(issueAfterSaving: boolean): void {
    if (this.form.invalid || this.lines.length === 0) {
      this.form.markAllAsTouched();
      this.error.set(
        this.lines.length === 0 ? 'Add at least one line.' : 'Check the highlighted fields.',
      );
      return;
    }

    const value = this.form.getRawValue();
    const lines: InvoiceLineInput[] = value.lines.map((l) => ({
      productId: l.productId,
      quantity: Number(l.quantity),
      unitPrice: Number(l.unitPrice),
      discountPercent: Number(l.discountPercent) || 0,
      taxRateId: l.taxRateId || null,
      exemptionCode: l.exemptionCode || null,
      description: l.description || null,
    }));

    const id = this.id();
    const request: Observable<Invoice> = id
      ? this.api.updateDraft(id, {
          customerId: value.customerId,
          paymentTermDays: value.paymentTermDays,
          lines,
        })
      : this.api.createDraft({
          type: 'Invoice',
          customerId: value.customerId,
          paymentTermDays: value.paymentTermDays,
          lines,
        });

    this.saving.set(true);
    this.error.set(null);
    request.subscribe({
      next: (invoice) =>
        issueAfterSaving
          ? this.issue(invoice.id)
          : this.router.navigate(['/documents', invoice.id]),
      error: (e: unknown) => this.fail(e),
    });
  }

  private issue(id: string): void {
    this.api.issue(id).subscribe({
      next: () => this.router.navigate(['/documents', id]),
      // The draft was saved; continue editing it so the user can fix what blocked issuing.
      error: (e: unknown) => {
        this.fail(e);
        if (!this.id()) {
          this.router.navigate(['/documents', id, 'edit']);
        }
      },
    });
  }

  private fail(error: unknown): void {
    this.error.set(problemMessage(error));
    this.saving.set(false);
  }

  private newLine(
    value?: Partial<{
      productId: string;
      description: string;
      quantity: number;
      unitPrice: number;
      discountPercent: number;
      taxRateId: string;
      exemptionCode: string;
    }>,
  ) {
    return this.fb.group({
      productId: [value?.productId ?? '', Validators.required],
      description: [value?.description ?? '', Validators.maxLength(200)],
      quantity: [value?.quantity ?? 1, [Validators.required, Validators.min(0.0001)]],
      unitPrice: [value?.unitPrice ?? 0, [Validators.required, Validators.min(0)]],
      discountPercent: [value?.discountPercent ?? 0, [Validators.min(0), Validators.max(100)]],
      taxRateId: [value?.taxRateId ?? '', Validators.required],
      exemptionCode: [value?.exemptionCode ?? ''],
    });
  }
}
