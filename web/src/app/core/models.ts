// Mirrors the API contracts (InvoiceFlow.Application DTOs). Enums travel as strings.

export type DocumentType = 'Invoice' | 'InvoiceReceipt' | 'CreditNote';
export type InvoiceStatus = 'Draft' | 'Issued' | 'Paid' | 'Voided';
export type TaxRateCode = 'Normal' | 'Intermediate' | 'Reduced' | 'Exempt';
export type ProductType = 'Goods' | 'Service';

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface Customer {
  id: string;
  name: string;
  nif: string;
  street: string;
  postalCode: string;
  city: string;
  country: string;
  email: string | null;
}

export type CustomerInput = Omit<Customer, 'id'>;

export interface TaxRate {
  id: string;
  code: TaxRateCode;
  description: string;
  percentage: number;
}

export interface ExemptionReason {
  code: string;
  description: string;
}

export interface Product {
  id: string;
  code: string;
  description: string;
  type: ProductType;
  unit: string;
  unitPrice: number;
  taxRateId: string;
}

export interface InvoiceSummary {
  id: string;
  type: DocumentType;
  status: InvoiceStatus;
  documentNumber: string | null;
  customerName: string;
  customerNif: string;
  issueDate: string | null;
  dueDate: string | null;
  grossTotal: number;
}

export interface InvoiceLine {
  lineNumber: number;
  productId: string;
  productCode: string;
  description: string;
  unit: string;
  quantity: number;
  unitPrice: number;
  discountPercent: number;
  taxCode: TaxRateCode;
  taxPercentage: number;
  exemptionCode: string | null;
  exemptionReason: string | null;
  netAmount: number;
}

export interface TaxSummary {
  code: TaxRateCode;
  percentage: number;
  taxableAmount: number;
  taxAmount: number;
}

export interface Invoice {
  id: string;
  type: DocumentType;
  status: InvoiceStatus;
  documentNumber: string | null;
  atcud: string | null;
  customerId: string;
  customer: { name: string; nif: string; street: string; postalCode: string; city: string; country: string };
  paymentTermDays: number;
  issueDate: string | null;
  dueDate: string | null;
  systemEntryDate: string | null;
  lines: InvoiceLine[];
  taxSummary: TaxSummary[];
  netTotal: number;
  taxTotal: number;
  grossTotal: number;
}

export interface InvoiceLineInput {
  productId: string;
  quantity: number;
  unitPrice: number | null;
  discountPercent: number;
  taxRateId: string | null;
  exemptionCode: string | null;
  description: string | null;
}

export interface InvoiceDraftInput {
  customerId: string;
  paymentTermDays: number;
  lines: InvoiceLineInput[];
}

export interface CreateInvoiceDraftInput extends InvoiceDraftInput {
  type: DocumentType;
}

/** RFC 9457 problem details as returned by the API. */
export interface ProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
  code?: string;
  errors?: Record<string, string[]>;
}

export const documentTypeLabels: Record<DocumentType, string> = {
  Invoice: 'Invoice (FT)',
  InvoiceReceipt: 'Invoice-receipt (FR)',
  CreditNote: 'Credit note (NC)',
};
