import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="shell">
      <nav class="nav" aria-label="Main">
        <a class="brand" routerLink="/">
          <svg viewBox="0 0 24 24" width="22" height="22" aria-hidden="true">
            <path d="M6 2h9l5 5v15H6z" fill="none" stroke="currentColor" stroke-width="1.6" />
            <path d="M9 12h7M9 15.5h7M9 8.5h3" stroke="currentColor" stroke-width="1.6" />
          </svg>
          InvoiceFlow
        </a>
        <a routerLink="/documents" routerLinkActive="active">Documents</a>
        <a routerLink="/customers" routerLinkActive="active">Customers</a>
      </nav>
      <main class="main">
        <router-outlet />
      </main>
    </div>
  `,
  styles: `
    .shell {
      display: grid;
      grid-template-columns: 13rem 1fr;
      min-height: 100vh;
    }
    .nav {
      display: flex;
      flex-direction: column;
      gap: 0.15rem;
      padding: 1.25rem 0.75rem;
      background: var(--ink);
    }
    .nav a {
      padding: 0.5rem 0.75rem;
      border-radius: var(--radius-sm);
      color: #c9d5e8;
      text-decoration: none;
      font-weight: 500;
    }
    .nav a:hover {
      color: #fff;
    }
    .nav a.active {
      background: rgb(255 255 255 / 0.1);
      color: #fff;
    }
    .nav .brand {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      margin-bottom: 1.25rem;
      color: #fff;
      font-weight: 700;
      font-size: var(--step-1);
    }
    .main {
      min-width: 0;
      padding: 2rem clamp(1rem, 4vw, 3rem);
      max-width: 72rem;
    }
    @media (max-width: 720px) {
      .shell {
        grid-template-columns: 1fr;
        grid-template-rows: auto 1fr;
      }
      .nav {
        flex-direction: row;
        align-items: center;
        flex-wrap: wrap;
        padding: 0.5rem 1rem;
        gap: 0.25rem;
      }
      .nav .brand {
        margin: 0 auto 0 0;
      }
    }
  `,
})
export class App {}
