import { Component, inject, input, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { Auth } from '../../core/auth';
import { problemMessage } from '../../core/problem';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule],
  template: `
    <main class="login">
      <section class="panel">
        <p class="brand">InvoiceFlow</p>
        <h1>Sign in</h1>

        @if (error()) {
          <p class="alert" role="alert">{{ error() }}</p>
        }

        <form [formGroup]="form" (ngSubmit)="submit()">
          <div class="field">
            <label for="email">Email</label>
            <input id="email" type="email" formControlName="email" autocomplete="username" />
          </div>
          <div class="field">
            <label for="password">Password</label>
            <input
              id="password"
              type="password"
              formControlName="password"
              autocomplete="current-password"
            />
          </div>
          <button class="btn btn-primary" type="submit" [disabled]="busy()">Sign in</button>
        </form>

        <details class="demo">
          <summary>Demo accounts</summary>
          <ul>
            @for (account of demoAccounts; track account.email) {
              <li>
                <button
                  type="button"
                  class="btn-link"
                  (click)="fill(account.email, account.password)"
                >
                  {{ account.role }}
                </button>
                <span class="muted">{{ account.email }}</span>
              </li>
            }
          </ul>
        </details>
      </section>
      <aside class="note" aria-hidden="true">
        <p>Invoices, credit notes and SAF-T for Portuguese small businesses.</p>
      </aside>
    </main>
  `,
  styles: `
    .login {
      display: grid;
      grid-template-columns: minmax(20rem, 28rem) 1fr;
      min-height: 100vh;
    }
    .panel {
      display: grid;
      align-content: center;
      gap: 1.25rem;
      padding: 3rem clamp(1.5rem, 5vw, 4rem);
      background: var(--sheet);
    }
    .brand {
      margin: 0;
      font-weight: 700;
    }
    form {
      display: grid;
      gap: 1rem;
    }
    form .btn {
      justify-content: center;
    }
    .demo summary {
      cursor: pointer;
      color: var(--muted);
    }
    .demo ul {
      margin: 0.5rem 0 0;
      padding: 0;
      list-style: none;
      display: grid;
      gap: 0.25rem;
    }
    .demo li {
      display: flex;
      gap: 0.5rem;
      align-items: baseline;
    }
    .demo .btn-link {
      cursor: pointer;
      font-weight: 600;
    }
    .note {
      display: grid;
      align-content: end;
      padding: 3rem;
      background: var(--ink)
        repeating-linear-gradient(0deg, transparent 0 31px, rgb(255 255 255 / 0.06) 31px 32px);
      color: #dfe7f3;
    }
    .note p {
      max-width: 22ch;
      margin: 0;
      font-size: var(--step-3);
      font-weight: 600;
      line-height: 1.15;
      letter-spacing: -0.02em;
    }
    @media (max-width: 760px) {
      .login {
        grid-template-columns: 1fr;
      }
      .note {
        display: none;
      }
    }
  `,
})
export class Login {
  private readonly auth = inject(Auth);
  private readonly router = inject(Router);

  readonly returnUrl = input<string>();

  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly form = inject(NonNullableFormBuilder).group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  protected readonly demoAccounts = [
    { role: 'Admin', email: 'admin@invoiceflow.local', password: 'Admin!2026' },
    { role: 'Accountant', email: 'accountant@invoiceflow.local', password: 'Accountant!2026' },
    { role: 'Viewer', email: 'viewer@invoiceflow.local', password: 'Viewer!2026' },
  ];

  protected fill(email: string, password: string): void {
    this.form.setValue({ email, password });
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.busy.set(true);
    this.error.set(null);
    const { email, password } = this.form.getRawValue();
    this.auth.login(email, password).subscribe({
      next: () => this.router.navigateByUrl(this.returnUrl() || '/'),
      error: (e: unknown) => {
        this.error.set(problemMessage(e));
        this.busy.set(false);
      },
    });
  }
}
