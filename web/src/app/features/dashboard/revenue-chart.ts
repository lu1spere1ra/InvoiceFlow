import { CurrencyPipe } from '@angular/common';
import { Component, computed, input, signal } from '@angular/core';

export interface MonthlyRevenue {
  year: number;
  month: number;
  netRevenue: number;
  documentCount: number;
}

const MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];

/**
 * Single-series column chart drawn in plain SVG (no chart library). Thin columns with a 4px rounded
 * data end anchored to the zero baseline, recessive grid, a hover/focus tooltip per column and a
 * table alternative for screen readers.
 */
@Component({
  selector: 'app-revenue-chart',
  imports: [CurrencyPipe],
  template: `
    <figure class="chart">
      <div class="plot">
        <svg
          [attr.viewBox]="'0 0 ' + width + ' ' + height"
          role="img"
          aria-label="Net revenue per month, last 12 months"
        >
          @for (tick of ticks(); track tick.value) {
            <line
              class="grid"
              [attr.x1]="left"
              [attr.x2]="width"
              [attr.y1]="tick.y"
              [attr.y2]="tick.y"
            />
            <text class="axis" [attr.x]="left - 8" [attr.y]="tick.y + 4" text-anchor="end">
              {{ tick.label }}
            </text>
          }
          <line
            class="baseline"
            [attr.x1]="left"
            [attr.x2]="width"
            [attr.y1]="zeroY()"
            [attr.y2]="zeroY()"
          />

          @for (bar of bars(); track bar.key; let i = $index) {
            <g
              class="bar"
              tabindex="0"
              [attr.aria-label]="bar.label + ': ' + (bar.value | currency)"
              (mouseenter)="active.set(i)"
              (mouseleave)="active.set(null)"
              (focus)="active.set(i)"
              (blur)="active.set(null)"
            >
              <!-- hit target wider and taller than the mark -->
              <rect
                class="hit"
                [attr.x]="bar.slotX"
                y="0"
                [attr.width]="slot()"
                [attr.height]="plotBottom"
              />
              <path
                class="mark"
                [class.dim]="active() !== null && active() !== i"
                [attr.d]="bar.path"
              />
              <text class="axis" [attr.x]="bar.cx" [attr.y]="height - 6" text-anchor="middle">
                {{ bar.short }}
              </text>
            </g>
          }
        </svg>

        @if (active() !== null) {
          @let bar = bars()[active()!];
          <div class="tooltip" role="status" [style.left.%]="(bar.cx / width) * 100">
            <strong>{{ bar.label }}</strong>
            <span>{{ bar.value | currency }}</span>
            <span class="muted"
              >{{ bar.count }} {{ bar.count === 1 ? 'document' : 'documents' }}</span
            >
          </div>
        }
      </div>

      <details class="as-table">
        <summary>Show as table</summary>
        <table class="table">
          <thead>
            <tr>
              <th>Month</th>
              <th class="num">Net revenue</th>
              <th class="num">Documents</th>
            </tr>
          </thead>
          <tbody>
            @for (bar of bars(); track bar.key) {
              <tr>
                <td>{{ bar.label }}</td>
                <td class="num">{{ bar.value | currency }}</td>
                <td class="num">{{ bar.count }}</td>
              </tr>
            }
          </tbody>
        </table>
      </details>
    </figure>
  `,
  styles: `
    .chart {
      margin: 0;
    }
    .plot {
      position: relative;
    }
    svg {
      display: block;
      width: 100%;
      height: auto;
      overflow: visible;
    }
    .grid {
      stroke: var(--rule);
      stroke-width: 1;
    }
    .baseline {
      stroke: var(--rule-strong);
      stroke-width: 1;
    }
    .axis {
      fill: var(--muted);
      font-size: 12px;
      font-variant-numeric: tabular-nums;
    }
    .hit {
      fill: transparent;
    }
    .mark {
      fill: var(--chart);
      transition: opacity 120ms ease;
    }
    .mark.dim {
      opacity: 0.45;
    }
    .bar:focus {
      outline: none;
    }
    .bar:focus-visible .hit {
      stroke: var(--focus);
      stroke-width: 2;
    }
    .tooltip {
      position: absolute;
      top: 0;
      transform: translateX(-50%);
      display: grid;
      gap: 0.1rem;
      padding: 0.45rem 0.65rem;
      background: var(--sheet);
      border: 1px solid var(--rule-strong);
      border-radius: var(--radius-sm);
      box-shadow: 0 6px 16px -10px rgb(19 41 75 / 0.4);
      font-size: var(--step--1);
      white-space: nowrap;
      pointer-events: none;
      font-variant-numeric: tabular-nums;
    }
    .as-table {
      margin-top: 0.75rem;
      font-size: var(--step--1);
    }
    .as-table summary {
      cursor: pointer;
      color: var(--muted);
    }
  `,
})
export class RevenueChart {
  readonly data = input.required<MonthlyRevenue[]>();
  protected readonly active = signal<number | null>(null);

  protected readonly width = 720;
  protected readonly height = 260;
  protected readonly left = 64;
  protected readonly plotTop = 16;
  protected readonly plotBottom = 228;

  /** Axis built from a round step (1, 2, 2.5 or 5 × 10ⁿ) so every tick label is a round number. */
  private readonly scale = computed(() => {
    const values = this.data().map((d) => d.netRevenue);
    const dataMax = Math.max(0, ...values);
    const dataMin = Math.min(0, ...values);
    const step = niceStep((dataMax - dataMin) / 4 || 1);
    const max = Math.ceil(dataMax / step) * step || step;
    const min = Math.floor(dataMin / step) * step;
    const span = max - min;
    return {
      max,
      min,
      step,
      y: (v: number) => this.plotTop + ((max - v) / span) * (this.plotBottom - this.plotTop),
    };
  });

  protected readonly zeroY = computed(() => this.scale().y(0));
  protected readonly slot = computed(
    () => (this.width - this.left) / Math.max(this.data().length, 1),
  );

  protected readonly ticks = computed(() => {
    const { max, min, step, y } = this.scale();
    const count = Math.round((max - min) / step);
    return Array.from({ length: count + 1 }, (_, i) => min + step * i).map((value) => ({
      value,
      y: y(value),
      label: compactEuro(value),
    }));
  });

  protected readonly bars = computed(() => {
    const { y } = this.scale();
    const slot = this.slot();
    const barWidth = Math.min(28, slot * 0.55);
    const zero = y(0);
    return this.data().map((d, i) => {
      const slotX = this.left + i * slot;
      const cx = slotX + slot / 2;
      const top = y(d.netRevenue);
      return {
        key: `${d.year}-${d.month}`,
        label: `${MONTHS[d.month - 1]} ${d.year}`,
        short: MONTHS[d.month - 1],
        value: d.netRevenue,
        count: d.documentCount,
        slotX,
        cx,
        path: columnPath(cx - barWidth / 2, barWidth, zero, top),
      };
    });
  });
}

/** Column with a 4px rounded data end and a square end on the baseline; handles negative values. */
function columnPath(x: number, w: number, zeroY: number, valueY: number): string {
  const h = Math.abs(zeroY - valueY);
  if (h < 0.5) {
    return '';
  }
  const r = Math.min(4, h, w / 2);
  if (valueY < zeroY) {
    return `M${x},${zeroY} V${valueY + r} Q${x},${valueY} ${x + r},${valueY} H${x + w - r} Q${x + w},${valueY} ${x + w},${valueY + r} V${zeroY} Z`;
  }
  return `M${x},${zeroY} V${valueY - r} Q${x},${valueY} ${x + r},${valueY} H${x + w - r} Q${x + w},${valueY} ${x + w},${valueY - r} V${zeroY} Z`;
}

function niceStep(rough: number): number {
  const magnitude = 10 ** Math.floor(Math.log10(rough));
  return [1, 2, 2.5, 5, 10].find((s) => s * magnitude >= rough)! * magnitude;
}

function compactEuro(value: number): string {
  const abs = Math.abs(value);
  const text =
    abs >= 1000 ? `${Number((abs / 1000).toFixed(abs >= 10000 ? 0 : 1))}k` : `${Math.round(abs)}`;
  return `${value < 0 ? '−' : ''}${text} €`;
}
