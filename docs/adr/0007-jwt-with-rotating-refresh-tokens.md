# ADR 0007 — JWT access tokens with rotating refresh tokens

- **Status:** Accepted
- **Date:** 2026-11-06

## Context

The API serves an Angular SPA and must authorize three roles (Admin, Accountant, Viewer). We want a
stateless API (no server sessions) but also sessions that survive a page reload and can be revoked.

## Decision

- **ASP.NET Core Identity** (`AddIdentityCore`, no cookie auth) stores users, roles and password hashes
  in the same database. Lockout after 5 failed attempts.
- **Access token:** a JWT signed with HMAC-SHA256, valid for **15 minutes**, carrying `sub`, `email`,
  `name` and role claims. The API validates it without touching the database.
- **Refresh token:** 64 random bytes, valid for **7 days**, **single use**. Only its SHA-256 hash is
  stored (`RefreshTokens` table), so a database leak does not yield usable tokens.
- **Rotation and reuse detection:** every refresh revokes the presented token and issues a new pair.
  If an already-revoked token is presented again, someone replayed or stole it, so **all** active
  refresh tokens of that user are revoked and they must sign in again.
- **Transport in the browser:**
  - The access token is returned in the response body and kept **in memory only** (an Angular signal),
    never in `localStorage`, so an XSS bug cannot exfiltrate a long-lived credential from storage.
  - The refresh token is sent as an **HttpOnly, SameSite=Strict cookie scoped to `/api/auth`**. Scripts
    cannot read it, and it is not sent with any other API request, which limits CSRF exposure to the
    refresh endpoint (which only returns a token to the same origin). `Secure` is set whenever the request
    is HTTPS.
  - On start-up the SPA calls `/api/auth/refresh` to restore the session from the cookie. An HTTP
    interceptor retries a request once after a 401 by refreshing first; concurrent 401s share one refresh.
- **Secure by default:** a fallback authorization policy requires an authenticated user on every endpoint.
  Endpoints opt out explicitly (`[AllowAnonymous]` on auth, `.AllowAnonymous()` on docs and health).
  Writes are restricted with `[Authorize(Roles = Roles.Writers)]`.

## Alternatives considered

- **Cookie authentication only:** simpler for a same-origin SPA, but the API is also used from Scalar and
  could serve other clients; bearer tokens keep it client-agnostic.
- **Refresh token in `localStorage`:** simpler, but readable by any injected script.
- **External identity provider (Entra ID, Keycloak):** the right call for a real product with SSO; too
  heavy for a self-contained demo.

## Consequences

- A revoked user keeps access for at most the access-token lifetime (15 minutes).
- The signing key must come from configuration/secrets (`Jwt__SigningKey`); the API refuses to start
  without a key of at least 32 characters.
