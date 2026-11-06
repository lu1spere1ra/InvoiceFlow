# InvoiceFlow — Implementation Plan

> Progress: phases 0–7 implemented on 2026-11-06 (see the git history). 140 backend tests (unit,
> architecture, integration with SQL Server) and 10 frontend tests, all green in CI.
>
> Status: **approved** (2026-11-06), with one review note: *keep it lean — no technology beyond what is
> needed, no over-engineering.* Section 10 lists what was cut as a result.

InvoiceFlow is a small invoicing ERP for Portuguese SMEs, built as a portfolio project. The goal is
code that a tech lead can read top to bottom and find deliberate, defensible decisions: a rich domain
model, clear layer boundaries, real tests, and a clean git history.

> **Disclaimer (will also be in the README):** InvoiceFlow is **not** certified invoicing software.
> The hash chain, ATCUD and SAF-T (PT) features are simplified implementations *inspired by* the
> Autoridade Tributária (AT) rules, for demonstration only.

---

## 1. Guiding principles

1. **Vertical first.** Phase 1 delivers a thin but complete slice (database → domain → API → UI →
   Docker) before any breadth is added.
2. **Business rules live in the Domain.** Entities protect their own invariants; no public setters on
   aggregates; the Application layer orchestrates, it does not decide.
3. **Green at every phase boundary.** `dotnet build` (warnings as errors), all tests and the Angular
   build must pass before moving to the next phase.
4. **Small Conventional Commits.** One logical change per commit (`feat:`, `fix:`, `test:`, `docs:`,
   `refactor:`, `build:`, `ci:`, `chore:`).
5. **No commercial-licence dependencies.** No MediatR, AutoMapper, MassTransit or FluentAssertions.
6. **Every non-obvious decision gets an ADR** in `docs/adr/`.

---

## 2. Solution structure

```
InvoiceFlow/
├── src/
│   ├── InvoiceFlow.Domain/          # Entities, value objects, domain services, domain errors. No dependencies.
│   ├── InvoiceFlow.Application/     # Use cases (command/query handlers), ports (interfaces), validators, DTOs.
│   ├── InvoiceFlow.Infrastructure/  # EF Core, Identity, JWT, QuestPDF, SAF-T writer, signing, clock.
│   └── InvoiceFlow.Api/             # Controllers, ProblemDetails mapping, auth setup, OpenAPI + Scalar, Serilog.
├── tests/
│   ├── InvoiceFlow.Domain.UnitTests/
│   ├── InvoiceFlow.Application.UnitTests/
│   ├── InvoiceFlow.Architecture.Tests/   # Layer-dependency rules (plain reflection, no library)
│   └── InvoiceFlow.Api.IntegrationTests/ # WebApplicationFactory + Testcontainers (SQL Server)
├── web/                              # Angular app (standalone components, signals)
├── docs/
│   ├── PLAN.md
│   └── adr/
├── docker-compose.yml
├── Directory.Build.props             # Nullable, ImplicitUsings, TreatWarningsAsErrors, analyzers, LangVersion
├── Directory.Packages.props          # Central Package Management (one place for all versions)
├── global.json                       # Pins the .NET 10 SDK
├── .editorconfig
└── .github/workflows/ci.yml
```

Dependency rule (enforced by architecture tests): `Api → Infrastructure → Application → Domain`;
`Domain` references nothing; `Application` does not reference EF Core or ASP.NET Core.

### Key libraries

| Concern | Choice | Licence |
|---|---|---|
| Web API | ASP.NET Core 10, controllers | MIT |
| Persistence | EF Core 10 + SQL Server 2022 (Docker) | MIT |
| Auth | ASP.NET Core Identity + JWT bearer | MIT |
| Validation | FluentValidation | Apache-2.0 |
| Logging | Serilog (structured console output) | Apache-2.0 |
| API docs | `Microsoft.AspNetCore.OpenApi` + Scalar | MIT |
| PDF | QuestPDF (Community licence) | free under USD 1M revenue |
| QR code | QRCoder | MIT |
| Tests | xUnit, Shouldly, NSubstitute, Testcontainers.MsSql | MIT/BSD/Apache |
| Frontend | Angular 22 (current stable), plain CSS, hand-rolled SVG charts | MIT |

---

## 3. Domain model (overview)

**Aggregates**

- **Customer** — name, NIF (validated with the Portuguese mod-11 check digit), address, email. The
  special "Consumidor final" NIF `999999990` is supported. Soft-deletable.
- **Product** (article or service) — code, description, unit price, unit of measure, type
  (product/service, needed for SAF-T `ProductType`), default VAT rate. Soft-deletable.
- **TaxRate** — code (`NOR` 23%, `INT` 13%, `RED` 6%, `ISE` 0%), percentage, and for exempt lines a
  mandatory **exemption reason** (AT code `M01`…`M99` + text). Mainland rates only (Azores/Madeira
  out of scope, documented).
- **DocumentSeries** — per document type and fiscal year (e.g. `FT 2026A`), holds the last number
  used and the **ATCUD validation code** (simulated "communication" to AT). Owns number allocation.
- **Invoice** (one aggregate, discriminated by `DocumentType`: `FT` invoice, `FR` invoice-receipt,
  `NC` credit note) — customer snapshot, lines, totals, status, number, hash, ATCUD.

**State machine**

```
Draft ──issue──► Issued ──pay──► Paid
  │                │
 delete          void (reason required)
  ▼                ▼
(removed)        Voided
```

- `FR` goes directly to `Paid` when issued (it is an invoice and a receipt at once).
- `NC` must reference an issued `FT`/`FR`; the credited amount cannot exceed what is still creditable.
- Only drafts can be edited or deleted. Once issued, the document is **immutable**: lines, totals,
  number, hash and customer snapshot can never change (enforced in the aggregate *and* by the
  persistence layer refusing updates to those columns).

**Money and rounding** — `decimal` everywhere; unit prices up to 4 decimals; line totals rounded to
2 decimals (`MidpointRounding.AwayFromZero`); VAT computed per rate on the document summary. The exact
rule goes in an ADR because it is the classic "1 cent off" interview question.

**Hash chain (simplified AT model)** — on issue, the document signs the string
`InvoiceDate;SystemEntryDate;DocumentNo;GrossTotal;PreviousHash` (same field layout as the AT spec)
with an RSA key (RSA-SHA1 PKCS#1 v1.5, as AT prescribes), chaining to the previous document of the
same series. The first document of a series uses an empty previous hash. Printed documents show the
4 characters at positions 1, 11, 21, 31 of the hash, as AT requires. Signing is behind an
`IDocumentSigner` port so the Domain stays crypto-agnostic and unit tests use a deterministic fake.

**ATCUD** — `{SeriesValidationCode}-{SequentialNumber}`, e.g. `CSDF7T5H-42`.

---

## 4. Architecture decisions to record (ADRs)

| # | ADR | Core decision |
|---|---|---|
| 0001 | Clean Architecture with a rich domain | 4 projects, dependency rule, invariants in entities |
| 0002 | Use-case classes instead of MediatR | One small class per use case, injected straight into controllers; validation runs in an MVC filter; licence + explicitness |
| 0003 | Document numbering strategy | Number assigned only at issue time (drafts have none → no gaps); allocation inside a transaction with optimistic concurrency (`rowversion`) on the series and retry |
| 0004 | Hash chain and ATCUD | Simplified AT model, RSA signing behind a port, what is *not* compliant |
| 0005 | Error handling: Result pattern + ProblemDetails | Expected failures are `Result<T>`/`Error` values mapped to RFC 9457 ProblemDetails; exceptions only for bugs |
| 0006 | Persistence boundaries | Aggregate repositories for writes, read-model query services (projections straight to DTOs) for lists/dashboard; manual mapping; soft delete via global query filters; audit via `SaveChanges` interceptor |
| (opt.) 0007 | Authentication: JWT + rotating refresh tokens | Short-lived access token, hashed refresh tokens, rotation, revoke-all on reuse |

---

## 5. Phases

Every phase ends with: `dotnet build -warnaserror`, `dotnet test`, `npm run build` (+ frontend tests
from phase 1), and `docker compose up` smoke check where relevant. Each phase is several small commits.

### Phase 0 — Foundations
- Repository hygiene: `.gitignore`, `.editorconfig`, `global.json`, `Directory.Build.props`,
  `Directory.Packages.props`, `LICENSE` (MIT).
- Empty solution with the 4 source projects and test projects, project references per the dependency rule.
- Architecture tests (layer rules) — they pass trivially now and guard every later phase.
- GitHub Actions workflow: restore, build, test.
- ADR 0001 (Clean Architecture).

**Done when:** CI green on an empty-but-wired solution.

### Phase 1 — MVP vertical slice (end to end)
Smallest useful flow: *create a customer → create a draft invoice with lines → issue it → see it with
its number in the UI*.
- Domain: `Customer` (with NIF validation), `TaxRate` (seeded), `DocumentSeries`, `Invoice` (type `FT`
  only), lines, totals, `Draft → Issued` transition, numbering. Unit tests for totals/VAT, NIF,
  transitions and numbering.
- Application: use-case classes `CreateCustomer`, `CreateDraftInvoice`, `IssueInvoice`,
  `GetInvoiceById`, `ListInvoices` (simple); FluentValidation; `Result<T>`.
- Infrastructure: `AppDbContext`, configurations, first migration, seed (VAT rates, default series,
  a few customers).
- API: controllers, ProblemDetails, Serilog request logging, OpenAPI + Scalar at `/scalar`.
- Integration tests with `WebApplicationFactory` + Testcontainers (create → issue → get).
- Web: Angular app scaffold, customer list, invoice list, invoice editor (draft + issue).
- Docker: API `Dockerfile`, web `Dockerfile` (build + nginx, proxying `/api`), `docker-compose.yml`
  with SQL Server; migrations + seed applied on startup in Development.
- ADRs 0002, 0003, 0005.

**Done when:** `docker compose up` → open `http://localhost:5100`, issue an invoice through the UI.

### Phase 2 — Authentication & authorization
- ASP.NET Core Identity (Guid keys) in Infrastructure; `/auth/login`, `/auth/refresh`, `/auth/logout`.
- JWT access token (15 min) + refresh token (7 days, stored hashed, rotated on every use; reusing a revoked token revokes all of the user's tokens).
- Roles: **Admin** (users, series, everything), **Accountant** (create/issue/void documents, catalog),
  **Viewer** (read-only). Policy-based authorization on controllers.
- Seeded demo users for each role (credentials in README).
- `ICurrentUser` port (used next phase by auditing).
- Angular: login page, auth service with signals, HTTP interceptor (attach token, silent refresh on
  401), route guards, role-aware UI.
- Integration tests: 401/403 matrix per role, refresh rotation and reuse detection.
- ADR 0007.

### Phase 3 — Catalog, full document lifecycle, listings
- Products/services CRUD; VAT exemption reasons; exempt lines require a reason.
- `FR` (invoice-receipt) and `NC` (credit note referencing the original, with creditable-amount rule).
- `Issued → Paid`, `Issued → Voided` (with reason); voided/paid rules for credit notes.
- Listing infrastructure: paging, filtering and sorting via a small, whitelisted query model (no
  dynamic LINQ strings from clients), returning a `PagedResult<T>`.
- Soft delete (`ISoftDeletable` + global query filter) for customers and products; documents are
  never deleted once issued.
- Audit interceptor: `CreatedAt/CreatedBy/UpdatedAt/UpdatedBy` filled from `ICurrentUser` + `TimeProvider`.
- Persistence-level immutability guard for issued documents.
- Angular: product pages, document type selector, credit-note flow, list filters/sort/paging.
- Unit + integration tests for all of the above. ADR 0006.

### Phase 4 — Compliance-inspired features: hash chain, ATCUD, PDF
- Series management (Admin): create series, "communicate" to get a validation code, close series.
- Hash chaining on issue via `IDocumentSigner` (RSA key from configuration; dev key generated and
  documented; never committed for "production").
- ATCUD on every issued document.
- `GET /invoices/{id}/pdf`: QuestPDF layout (issuer, customer, lines, VAT summary, exemption reasons,
  hash excerpt, ATCUD, QR code following the AT QR field specification).
- Unit tests: hash chain continuity, tamper detection (verifying the chain), ATCUD format, QR payload.
- ADR 0004.

### Phase 5 — SAF-T (PT) export
- `GET /saft?from=2026-01-01&to=2026-03-31` → XML file (`AuditFile` 1.04_01): Header,
  MasterFiles (Customer, Product, TaxTable), SourceDocuments/SalesInvoices with totals and hashes.
- Streaming writer with `XmlWriter` (no giant in-memory DOM).
- Try to obtain the official XSD (AT portal). If available, commit it under `tests/` and validate the
  generated file with `XmlSchemaSet` in an integration test. If the download is not possible from the
  build environment, I'll flag it and ask you to provide the file.

### Phase 6 — Dashboard
- Query services (read side, projections only): revenue by month (last 12 months, net of credit
  notes), top 5 customers, outstanding amounts (issued, unpaid) with ageing buckets.
- Angular dashboard page: KPI cards + simple SVG bar chart (no chart library).
- Integration tests on aggregated figures with a known dataset.

### Phase 7 — Polish & presentation
- Rich, realistic seed data (several months of documents so the dashboard looks alive).
- README: pitch, disclaimer, screenshots (taken with Playwright), Mermaid architecture diagram,
  how to run (Docker and local), demo credentials, technical decisions summary linking to ADRs.
- CI: add frontend lint/test/build and Docker image builds (API + web) on every push.
- Final review pass: dead code, naming, analyzers clean, ADRs consistent with code.

---

## 6. Runtime & ports

All host ports are in **5100–5199**:

| Service | Host port | Container |
|---|---|---|
| Web (nginx serving Angular, proxies `/api` → API) | **5100** | 80 |
| API (direct access, Scalar at `/scalar`) | **5101** | 8080 |
| SQL Server | **5102** | 1433 |

`docker compose up` builds and starts web, API and database; the API waits for the DB health check,
applies migrations and seeds demo data.

---

## 7. Testing strategy

- **Domain unit tests** (fast, no mocks needed): totals & VAT rounding, exemption rules, state
  transitions (valid and invalid), numbering, hash chain, ATCUD, NIF validation.
- **Application unit tests**: use-case orchestration with NSubstitute fakes, validators.
- **Architecture tests**: dependency rule, no EF Core in Application/Domain, naming conventions.
- **API integration tests**: one SQL Server container per test run (Testcontainers), a fresh database
  per test class, real HTTP through `WebApplicationFactory`, real JWTs.
- **Frontend**: unit tests for services/critical components (Angular's default runner).

---

## 8. Risks & open points

1. **Build environment of this session.** The .NET SDK download host is blocked by the session's
   network policy and the Docker daemon is not running. I'll try the Microsoft apt feed (reachable)
   and starting `dockerd`; if Testcontainers cannot run here, integration tests will still run in
   GitHub Actions, and I'll say so explicitly instead of claiming green.
2. **SAF-T XSD availability** (see Phase 5).
3. **QuestPDF licence:** Community licence is free for individuals and companies under USD 1M annual
   revenue — fine for a portfolio; noted in the README.
4. **Scope guard:** receipts (`RG`), multi-currency, multi-company, Azores/Madeira VAT and AT web
   service communication are explicitly out of scope.

## 9. Decisions I'd like you to confirm

Defaults below are what I'll use unless you say otherwise:

Confirmed: Phase 1 without auth; RSA-SHA1 signing to mirror AT; ports as in section 6.
UI kit: **none** — plain CSS keeps the frontend lean (see section 10).

## 10. Simplifications after review

| Cut | Replaced by |
|---|---|
| Handler interfaces + Scrutor decorators | Plain use-case classes registered explicitly; a single MVC action filter runs FluentValidation |
| NetArchTest | ~30 lines of reflection over assembly references |
| Respawn | A fresh database per test class in the same container |
| Angular Material, chart library | Plain CSS and a tiny SVG bar chart component |
| Seq | Serilog console output (structured, readable with `docker compose logs`) |
| Refresh-token "families" | Rotation + revoke all of the user's refresh tokens on reuse |
