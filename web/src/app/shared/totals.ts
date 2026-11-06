import { TaxSummary } from '../core/models';

/** Same rule as the domain (Money.Round): two decimals, half away from zero. */
export function roundMoney(amount: number): number {
  // toPrecision strips binary noise first: 1.005 * 100 is 100.49999999999999 in floating point.
  const cents = Number((Math.abs(amount) * 100).toPrecision(15));
  return (Math.sign(amount) * Math.round(cents)) / 100;
}

export interface LineForTotals {
  quantity: number;
  unitPrice: number;
  discountPercent: number;
  taxPercentage: number;
}

/**
 * Live preview of document totals while editing a draft. The API is authoritative; this mirrors its
 * rule (net rounded per line, VAT rounded once per rate) so the preview matches what will be saved.
 */
export function previewTotals(lines: LineForTotals[]) {
  const nets = lines.map((l) => ({
    net: roundMoney(l.quantity * l.unitPrice * (1 - l.discountPercent / 100)),
    rate: l.taxPercentage,
  }));

  const byRate = new Map<number, number>();
  for (const { net, rate } of nets) {
    byRate.set(rate, (byRate.get(rate) ?? 0) + net);
  }

  const taxes: Pick<TaxSummary, 'percentage' | 'taxableAmount' | 'taxAmount'>[] = [...byRate.entries()]
    .sort(([a], [b]) => b - a)
    .map(([percentage, taxable]) => ({
      percentage,
      taxableAmount: roundMoney(taxable),
      taxAmount: roundMoney((taxable * percentage) / 100),
    }));

  const netTotal = roundMoney(nets.reduce((sum, l) => sum + l.net, 0));
  const taxTotal = roundMoney(taxes.reduce((sum, t) => sum + t.taxAmount, 0));
  return { lineNets: nets.map((l) => l.net), taxes, netTotal, taxTotal, grossTotal: roundMoney(netTotal + taxTotal) };
}
