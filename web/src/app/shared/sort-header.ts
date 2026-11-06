import { Component, WritableSignal, computed, input } from '@angular/core';

export interface SortState {
  field: string;
  descending: boolean;
}

/** Column header that toggles server-side sorting; announces the current order to screen readers. */
@Component({
  selector: 'app-sort-header',
  template: `
    <button type="button" (click)="toggle()" [attr.aria-label]="ariaLabel()">
      {{ label() }}
      <span class="arrow" aria-hidden="true">{{ arrow() }}</span>
    </button>
  `,
  styles: `
    button {
      display: inline-flex;
      gap: 0.25rem;
      padding: 0;
      border: none;
      background: none;
      color: inherit;
      font: inherit;
      cursor: pointer;
    }
    .arrow {
      min-width: 0.75em;
      color: var(--ink);
    }
  `,
})
export class SortHeader {
  readonly label = input.required<string>();
  readonly field = input.required<string>();
  readonly sort = input.required<WritableSignal<SortState>>();
  /** Direction used the first time the column is selected. */
  readonly firstDescending = input(false);

  protected readonly active = computed(() => this.sort()().field === this.field());
  protected readonly arrow = computed(() =>
    this.active() ? (this.sort()().descending ? '↓' : '↑') : '',
  );
  protected readonly ariaLabel = computed(() =>
    this.active()
      ? `${this.label()}, sorted ${this.sort()().descending ? 'descending' : 'ascending'}`
      : `Sort by ${this.label()}`,
  );

  protected toggle(): void {
    const current = this.sort()();
    this.sort().set(
      current.field === this.field()
        ? { field: current.field, descending: !current.descending }
        : { field: this.field(), descending: this.firstDescending() },
    );
  }
}
