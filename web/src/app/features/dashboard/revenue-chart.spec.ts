import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { RevenueChart } from './revenue-chart';

const months = (values: number[]) =>
  values.map((netRevenue, i) => ({ year: 2026, month: i + 1, netRevenue, documentCount: 1 }));

function render(values: number[]): HTMLElement {
  const fixture = TestBed.createComponent(RevenueChart);
  fixture.componentRef.setInput('data', months(values));
  fixture.detectChanges();
  return fixture.nativeElement as HTMLElement;
}

describe('RevenueChart', () => {
  it('uses round tick labels above the maximum', () => {
    const labels = [...render([3000, 13700, 9800]).querySelectorAll('svg > text')].map((t) =>
      t.textContent?.trim(),
    );

    expect(labels).toEqual(['0 €', '5k €', '10k €', '15k €']);
  });

  it('draws one labelled, focusable column per month', () => {
    const bars = render([100, 200, 300]).querySelectorAll('g.bar');

    expect(bars.length).toBe(3);
    expect(bars[1].getAttribute('tabindex')).toBe('0');
    expect(bars[1].getAttribute('aria-label')).toContain('Feb 2026');
  });

  it('extends the axis below zero when a month is negative', () => {
    const labels = [...render([1000, -400]).querySelectorAll('svg > text')].map((t) =>
      t.textContent?.trim(),
    );

    expect(labels[0]).toBe('−500 €');
  });
});
