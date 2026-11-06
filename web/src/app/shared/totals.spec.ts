import { describe, expect, it } from 'vitest';
import { previewTotals, roundMoney } from './totals';

describe('roundMoney', () => {
  it('rounds half away from zero like the backend', () => {
    expect(roundMoney(0.125)).toBe(0.13);
    expect(roundMoney(1.005)).toBe(1.01);
    expect(roundMoney(-0.125)).toBe(-0.13);
  });
});

describe('previewTotals', () => {
  it('matches the backend example with discount', () => {
    const totals = previewTotals([
      { quantity: 3, unitPrice: 19.99, discountPercent: 10, taxPercentage: 23 },
    ]);

    expect(totals.netTotal).toBe(53.97);
    expect(totals.taxTotal).toBe(12.41);
    expect(totals.grossTotal).toBe(66.38);
  });

  it('rounds VAT once per rate, not per line', () => {
    const line = { quantity: 1, unitPrice: 0.1, discountPercent: 0, taxPercentage: 23 };

    expect(previewTotals([line, line, line]).taxTotal).toBe(0.07);
  });

  it('groups VAT by rate, highest first', () => {
    const totals = previewTotals([
      { quantity: 1, unitPrice: 100, discountPercent: 0, taxPercentage: 6 },
      { quantity: 1, unitPrice: 100, discountPercent: 0, taxPercentage: 23 },
    ]);

    expect(totals.taxes.map((t) => t.percentage)).toEqual([23, 6]);
    expect(totals.grossTotal).toBe(229);
  });
});
