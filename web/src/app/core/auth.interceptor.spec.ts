import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { beforeEach, describe, expect, it } from 'vitest';
import { Auth } from './auth';
import { authInterceptor } from './auth.interceptor';

const session = (token: string) => ({
  accessToken: token,
  expiresAt: '2030-01-01T00:00:00Z',
  user: { id: '1', email: 'a@b.pt', fullName: 'Ana', roles: ['Admin'] },
});

describe('authInterceptor', () => {
  let http: HttpClient;
  let backend: HttpTestingController;
  let auth: Auth;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([{ path: 'login', children: [] }]),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpClient);
    backend = TestBed.inject(HttpTestingController);
    auth = TestBed.inject(Auth);
  });

  it('attaches the access token to API calls', () => {
    auth.login('a@b.pt', 'x').subscribe();
    backend.expectOne('/api/auth/login').flush(session('token-1'));

    http.get('/api/invoices').subscribe();

    expect(backend.expectOne('/api/invoices').request.headers.get('Authorization')).toBe(
      'Bearer token-1',
    );
  });

  it('refreshes once on 401 and retries with the new token', () => {
    let result: unknown;
    http.get('/api/invoices').subscribe((r) => (result = r));

    backend.expectOne('/api/invoices').flush(null, { status: 401, statusText: 'Unauthorized' });
    backend.expectOne('/api/auth/refresh').flush(session('token-2'));

    const retry = backend.expectOne('/api/invoices');
    expect(retry.request.headers.get('Authorization')).toBe('Bearer token-2');
    retry.flush({ items: [] });
    expect(result).toEqual({ items: [] });
  });

  it('gives up when the refresh fails', () => {
    let failed = false;
    http.get('/api/invoices').subscribe({ error: () => (failed = true) });

    backend.expectOne('/api/invoices').flush(null, { status: 401, statusText: 'Unauthorized' });
    backend.expectOne('/api/auth/refresh').flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(failed).toBe(true);
    expect(auth.user()).toBeNull();
    backend.verify();
  });
});
