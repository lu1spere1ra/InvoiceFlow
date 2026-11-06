import { Component, computed, input } from '@angular/core';
import { InvoiceStatus } from '../core/models';

/** Document status shown as coloured text with a mark, so it does not rely on colour alone. */
@Component({
  selector: 'app-status-badge',
  template: `<span class="status" [class]="status().toLowerCase()">{{ label() }}</span>`,
  styles: `
    .status {
      font-weight: 600;
      font-size: var(--step--1);
      white-space: nowrap;
    }
    .status::before {
      margin-right: 0.35rem;
    }
    .draft {
      color: var(--draft);
    }
    .draft::before {
      content: '○';
    }
    .issued {
      color: var(--ink-soft);
    }
    .issued::before {
      content: '●';
    }
    .paid {
      color: var(--paid);
    }
    .paid::before {
      content: '✓';
    }
    .voided {
      color: var(--stamp);
      text-decoration: line-through;
    }
    .voided::before {
      content: '✕';
    }
  `,
})
export class StatusBadge {
  readonly status = input.required<InvoiceStatus>();
  protected readonly label = computed(() => this.status());
}
