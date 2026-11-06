import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, catchError, finalize, map, of, shareReplay, tap } from 'rxjs';

export interface User {
  id: string;
  email: string;
  fullName: string;
  roles: string[];
}

interface Session {
  accessToken: string;
  expiresAt: string;
  user: User;
}

/**
 * Holds the session in memory only. The access token never touches localStorage; after a reload the
 * session is restored from the HttpOnly refresh cookie, which scripts cannot read (ADR 0007).
 */
@Injectable({ providedIn: 'root' })
export class Auth {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly session = signal<Session | null>(null);
  private refreshInFlight: Observable<boolean> | null = null;

  readonly user = computed(() => this.session()?.user ?? null);
  readonly accessToken = computed(() => this.session()?.accessToken ?? null);
  readonly canWrite = computed(() => this.hasAnyRole('Admin', 'Accountant'));

  hasAnyRole(...roles: string[]): boolean {
    const userRoles = this.user()?.roles ?? [];
    return roles.some((role) => userRoles.includes(role));
  }

  login(email: string, password: string): Observable<User> {
    return this.http.post<Session>('/api/auth/login', { email, password }).pipe(
      tap((session) => this.session.set(session)),
      map((session) => session.user),
    );
  }

  /** Exchanges the refresh cookie for a new session. Concurrent callers share one request. */
  refresh(): Observable<boolean> {
    this.refreshInFlight ??= this.http.post<Session>('/api/auth/refresh', null).pipe(
      tap((session) => this.session.set(session)),
      map(() => true),
      catchError(() => {
        this.session.set(null);
        return of(false);
      }),
      finalize(() => (this.refreshInFlight = null)),
      shareReplay(1),
    );
    return this.refreshInFlight;
  }

  logout(): void {
    this.http
      .post('/api/auth/logout', null)
      .subscribe({ complete: () => undefined, error: () => undefined });
    this.session.set(null);
    this.router.navigate(['/login']);
  }
}
