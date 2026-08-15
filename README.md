# EventHub

A backend API for an event management platform. Users register for event sessions, organizations publish events, and payments/reviews complete the lifecycle.

> **Status:** Authentication is complete and tested. The event domain (events, registrations, payments, reviews) is modeled but not yet implemented. See [Roadmap](#roadmap).

## Tech Stack

- **.NET 10** — ASP.NET Core Web API
- **Entity Framework Core 10** + **SQL Server**
- **ASP.NET Core Identity** — Guid keys, roles, password hashing
- **JWT** access tokens + rotating refresh tokens
- **FluentValidation** — request validation
- **MailKit** — transactional email (SMTP)

## Architecture

Clean Architecture with a strict dependency direction: **API → Application → Domain**, with Infrastructure implementing the Application/Infrastructure-facing contracts.

```
EventHub.API/             Presentation. Controllers, middleware, rate limiting, config.
EventHub.Application/     Use cases, DTOs, services, validators, contracts (IUnitOfWork, IGenericRepository, service interfaces).
EventHub.Domain/          Entities, enums, roles, domain interfaces (ISoftDelete). No dependencies.
EventHub.Infrastructure/  EF Core, migrations, DbContext, GenericRepository, UnitOfWork, seeding.
```

Key structural decisions:

- **Repository + Unit of Work**: `IGenericRepository<T>` wraps DbSet access; `IUnitOfWork` exposes repositories and coordinates `SaveChanges`/transactions. Repositories deliberately do **not** auto-save on every mutation — the caller commits through the UoW, keeping write atomicity under the caller's control.
- **Soft delete everywhere**: every entity implements `ISoftDelete`, and `ApplicationDbContext` installs `HasQueryFilter(!IsDeleted)` per entity, so deleted rows are automatically excluded from all queries.
- **Identity with Guid keys**: `ApplicationUser : IdentityUser<Guid>`, `IdentityRole<Guid>`, `IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>`.

## Domain Model

| Entity | Purpose |
|---|---|
| `ApplicationUser` | Identity user + profile, soft-delete flags, email-confirmation code (3 min expiry, 5-attempt lockout), password-reset code (same policy) |
| `RefreshToken` | Single-use refresh token bound to a JWT (`JwtId`), `IsUsed`/`IsRevoked`, 7-day lifetime |
| `Category` | Event categorization |
| `Organization` | Event publisher; owns exactly one `ApplicationUser`; `Status` (`Active`/`Inactive`/`Pending`); requires verification document |
| `Event` | Published event owned by an `Organization`, categorized by `Category`; `EventStatus` (`Pending`/…) |
| `EventSession` | A dated/located session of an `Event` with a `Capacity`; users register for sessions |
| `Registration` | A user's booking of an `EventSession` (`RegistrationStatus`); owns optional 1:1 `Payment` and `Ticket` |
| `Payment` | Money for a registration (`PaymentStatus`) |
| `Ticket` | Issued per confirmed registration |
| `Review` | User review of an `Event` |
| `Favorite` | Composite key `(UserId, EventId)`; a user's saved events |

Relationships are configured in `ApplicationDbContext.OnModelCreating`. Notable delete behaviors: sessions/reviews/favorites/tickets/payments cascade off their parent; user- and organization-owned rows use `Restrict` to protect history.

## Authentication

Three roles: `User`, `OrganizationAdmin`, `SuperAdmin`.

### Flows

- **User registration** — validates → uploads profile photo → creates Identity user → assigns `User` role → emails a 6-digit confirmation code (3-min expiry). The code is **never** returned in the response; it lives only in the email and the DB.
- **Organization registration** — same, plus org name uniqueness, logo + verification PDF upload, and org + user creation inside a **transaction**. Files are uploaded before the transaction and deleted as compensation on failure. The organization is created `Pending` until a `SuperAdmin` approves it (not yet implemented).
- **Email confirmation** — 6-digit code, 5 failed-attempt lockout; success clears the code and logs the user in (issues a token pair).
- **Login** — email + password (only for confirmed accounts).
- **Refresh** — see below.
- **Logout** — revokes the presented refresh token.
- **Forgot / reset password** — `forgot-password` emails a reset code (same lockout policy); `reset-password` verifies the code, pre-validates the new password, and rotates the password via `RemovePasswordAsync` + `AddPasswordAsync`. Both endpoints return identical responses whether or not the email exists (anti-enumeration).

### JWT + Refresh Tokens

- Access token: HS256-signed, carries `NameIdentifier`, roles, `Jti`, ~`DurationInMinutes` expiry.
- Refresh token: unguessable random value, persisted in DB, bound to the access token's `Jti`, valid 7 days, single-use.
- On refresh, the **DB is the source of truth**: token looked up first, checked for used/revoked/expired, then the JWT is validated *without* the lifetime check (an expired access token is the expected input). The access token must be genuinely expired. Rotation marks the old token consumed before issuing a new pair.
- Known limitations (accepted for now): replay of a *used* token is indistinguishable from logout revocation, so token-theft detection/family revocation is not implemented; access tokens remain valid until expiry after a password change; refresh tokens are never cleaned up.

### Security decisions

- `SignIn.RequireConfirmedEmail = true`; `RequireUniqueEmail = true`; strict password policy.
- Email-confirmation and password-reset codes are brute-force limited by both a 5-attempt per-account counter **and** a fixed-window rate limiter on the endpoints.
- Failed-attempt counters are per-code-type (`EmailConfirmationCodeAttempts` vs `PasswordResetCodeAttempts`) so one flow cannot exhaust the other's budget.
- File uploads validate extension + size, and are deleted (compensated) when a later DB step fails.
- Rate limiting: fixed window (`1 request / 10 s`, queue 2). Note: all code endpoints currently share one global bucket — a per-user partition key is a known improvement.

## Getting Started

### Prerequisites

- .NET 10 SDK
- SQL Server (local or container)

### Configuration

Connection string, JWT settings, email settings, and the seeded SuperAdmin are stored in **user secrets** (not committed). Set them for `EventHub.API`:

```bash
dotnet user-secrets init --project EventHub.API
dotnet user-secrets set --project EventHub.API "ConnectionStrings:DefaultConnection" "Server=localhost;Database=EventHub;Trusted_Connection=True;TrustServerCertificate=True"
dotnet user-secrets set --project EventHub.API "JWT:Key" "<256-bit secret>"
dotnet user-secrets set --project EventHub.API "JWT:Issuer" "EventHub"
dotnet user-secrets set --project EventHub.API "JWT:Audience" "EventHubUsers"
dotnet user-secrets set --project EventHub.API "JWT:DurationInMinutes" "15"
dotnet user-secrets set --project EventHub.API "EmailSettings:FromEmail" "<gmail-address>"
dotnet user-secrets set --project EventHub.API "EmailSettings:DisplayName" "EventHub"
dotnet user-secrets set --project EventHub.API "EmailSettings:Password" "<gmail-app-password>"
```

Email uses Gmail SMTP (`smtp.gmail.com:587`) with a Gmail **App Password** — never your login password.

### Database

```bash
dotnet ef database update -p EventHub.Infrastructure -s EventHub.API
```

### Seeding

Roles are created automatically when the seeder runs. To seed a `SuperAdmin`, uncomment the block in `Program.cs` (`DbSeeder.SeedAsync`) and provide the `SuperAdmin:Email`, `SuperAdmin:Password`, `SuperAdmin:FirstName`, `SuperAdmin:LastName` user secrets (the seeder has dev fallbacks).

### Run

```bash
dotnet run --project EventHub.API
```

OpenAPI (Swagger UI) is available at `/swagger` in development.

## API Surface

| Method | Route | Auth | Purpose |
|---|---|---|---|
| POST | `/api/auth/register` | — | Register a user (multipart form, optional photo) |
| POST | `/api/auth/confirm-code` | — | Confirm email with 6-digit code (logs in) |
| POST | `/api/auth/resend-code` | — | Resend confirmation code |
| POST | `/api/auth/login` | — | Login (confirmed accounts only) |
| POST | `/api/auth/refresh-token` | — | Rotate token pair |
| POST | `/api/auth/logout` | — | Revoke refresh token |
| POST | `/api/auth/register-organization` | — | Register an organization (multipart form) |
| POST | `/api/auth/forgot-password` | — | Email a password-reset code |
| POST | `/api/auth/reset-password` | — | Set new password using reset code |

All DTOs are validated with FluentValidation; validation failures and business errors return structured `AuthResponseDto` payloads.

## Roadmap

1. **Categories** — CRUD (management).
2. **Organizations** — SuperAdmin approval workflow for `Pending` organizations.
3. **Events + EventSessions** — publish/manage by `OrganizationAdmin`.
4. **Registrations → Payments → Tickets** — the transactional core.
5. **Reviews + Favorites** — social features.

## Notes for Contributors

- Write migrations only from `EventHub.Infrastructure` (`-p EventHub.Infrastructure -s EventHub.API`).
- **Review the generated migration before applying it.** If it does not match intent and was never applied, delete it and regenerate rather than stacking fix-up migrations.
- Follow the existing layering: controllers stay thin, business rules live in Application services, persistence stays in Infrastructure, and Domain has zero dependencies.
- Keep entities non-nullable where the DB guarantees presence; nullable reference types are enabled across the solution.
