# 🔐 Auth — ASP.NET Core Authentication & Authorization API

A production-style **.NET 10** backend that implements a complete, secure authentication & authorization flow: user and organization registration, email confirmation with OTP codes, JWT + rotating refresh tokens, password recovery, and Google OAuth sign-in.

> This project was originally part of a larger event-management platform. I scoped it down to build a **standalone, reusable Auth feature** that can be dropped into (or extended to) any project.

---

## ✨ What This Project Does

A full, real-world auth backend — not just a login endpoint. Everything a modern app needs to onboard, secure, and recover users:

- **User registration** with profile-photo upload
- **Organization registration** with logo + verification-document upload (transactional, `Pending` approval workflow)
- **Email confirmation** via 6-digit OTP codes sent through Gmail SMTP (MailKit)
- **Login** (confirmed accounts only) issuing a JWT access token + refresh token
- **Refresh token rotation** — single-use, DB-backed, replay-safe
- **Logout** — revokes the presented refresh token
- **Forgot / reset password** with OTP verification and atomic rotation
- **Google OAuth** sign-in (`/api/auth/login-google` → callback)
- **Role-based authorization** — `User`, `OrganizationAdmin`, `SuperAdmin`
- **Rate limiting** on all OTP endpoints (brute-force protection)

---

## 🛠️ Tech Stack

| Layer | Tech |
|---|---|
| Runtime | .NET 10 — ASP.NET Core Web API |
| Data | Entity Framework Core 10 + SQL Server |
| Identity | ASP.NET Core Identity (Guid keys, roles, hashing) |
| Auth tokens | JWT (HS256) + rotating refresh tokens |
| Validation | FluentValidation |
| Email | MailKit (Gmail SMTP) |
| OAuth | Google (`Microsoft.AspNetCore.Authentication.Google`) |

---

## 🧱 Architecture

**Clean Architecture** with a strict dependency direction — **API → Application → Domain**, Infrastructure implements the contracts:

```
Auth.API/             Presentation. Controllers, rate limiting, config.
Auth.Application/     Use cases, DTOs, services, validators, contracts.
Auth.Domain/          Entities, enums, roles, domain interfaces. No dependencies.
Auth.Infrastructure/  EF Core, migrations, DbContext, repositories, seeding.
```

**Key structural decisions:**

- **Repository + Unit of Work** — `IGenericRepository<T>` wraps `DbSet` access; `IUnitOfWork` coordinates `SaveChanges` and transactions. Repositories never auto-save; the caller commits, keeping writes atomic.
- **Soft delete everywhere** — every entity implements `ISoftDelete`, and the `DbContext` installs a global `HasQueryFilter(!IsDeleted)`, automatically excluding deleted rows from all queries.
- **Identity with `Guid` keys** — `ApplicationUser : IdentityUser<Guid>`, `IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>`.
- **Compensation on failure** — uploaded files are deleted if a later DB step fails, avoiding orphaned data.

---

## 📦 What I Implemented

### Authentication flows
- **User registration** → validate → upload profile photo → create Identity user → assign `User` role → email a 6-digit confirmation code (3-min expiry). The code is **never returned in the API response** — it only lives in the email and the DB.
- **Organization registration** → the same, plus org-name uniqueness, logo + verification-PDF upload, and **atomic** org + user creation inside a `BEGIN TRANSACTION`. Files are uploaded first and removed as compensation if anything fails. The org starts `Pending` until a `SuperAdmin` approves it.
- **Email confirmation** → 6-digit code with a **5-attempt lockout**; success clears the code and logs the user in by issuing a fresh token pair.
- **Login** → email + password (confirmed accounts only).
- **Logout** → revokes the presented refresh token in the DB.

### JWT + refresh token rotation
- Access token: HS256-signed, carries `NameIdentifier`, roles, and a `Jti`. Short-lived (`DurationInMinutes`).
- Refresh token: unguessable random value, stored in the DB, **bound to the access token's `Jti`**, valid 7 days, **single-use**.
- On refresh, **the DB is the source of truth** — the token is looked up first and checked for used/revoked/expired, then the JWT is validated *without* the lifetime check (an expired access token is expected input), and the access token must be genuinely expired. Rotation **consumes the old token before issuing the new pair**, so it can never be replayed.

### Password recovery
- **Forgot password** emails a reset code (same 5-attempt lockout policy).
- **Reset password** verifies the code, then rotates the password via `RemovePasswordAsync` + `AddPasswordAsync`, issues a fresh token pair, and commits.
- Both endpoints return **identical responses whether or not the email exists** — a deliberate anti-enumeration measure.

### Security decisions
- `SignIn.RequireConfirmedEmail = true`, `RequireUniqueEmail = true`, strict password policy.
- OTP endpoints protected by **both** a 5-attempt per-account counter **and** a fixed-window rate limiter (`1 request / 10 s`, queue 2).
- Per-code-type attempt counters (`EmailConfirmationCodeAttempts` vs `PasswordResetCodeAttempts`) so one flow can't exhaust the other's budget.
- File uploads validate extension + size and are cleaned up on failure.

---

## 📂 Project Structure

```
Auth.slnx
├── Auth.API/                  # Web API entry point
│   ├── Controllers/AuthController.cs
│   ├── Extensions/RateLimiterExtension.cs
│   └── Program.cs
├── Auth.Application/          # Business logic
│   ├── Dtos/AuthDtos/         # Request/response DTOs
│   ├── Helper/                # JWT + EmailSettings config
│   ├── Interfaces/            # IUnitOfWork, IGenericRepository, ITransaction
│   ├── Services/              # Auth/Email/File/Jwt services
│   └── Validators/Auth/       # FluentValidation rules
├── Auth.Domain/               # Entities, enums, roles, ISoftDelete
│   └── Entities/              # ApplicationUser, Organization, RefreshToken
└── Auth.Infrastructure/       # Persistence
    ├── Data/                  # ApplicationDbContext + DbSeeder
    ├── Migrations/
    ├── Repositories/
    └── UnitOfWork/
```

---

## 🚀 Getting Started

### Prerequisites
- .NET 10 SDK
- SQL Server (local or container)

### Configuration

Connection string, JWT settings, email settings, Google OAuth, and the seeded SuperAdmin are stored in **user secrets** (never committed):

```bash
dotnet user-secrets init --project Auth.API
dotnet user-secrets set --project Auth.API "ConnectionStrings:DefaultConnection" "Server=localhost;Database=Auth;Trusted_Connection=True;TrustServerCertificate=True"
dotnet user-secrets set --project Auth.API "JWT:Key" "<256-bit secret>"
dotnet user-secrets set --project Auth.API "JWT:Issuer" "Auth"
dotnet user-secrets set --project Auth.API "JWT:Audience" "AuthUsers"
dotnet user-secrets set --project Auth.API "JWT:DurationInMinutes" "15"
dotnet user-secrets set --project Auth.API "EmailSettings:FromEmail" "<gmail-address>"
dotnet user-secrets set --project Auth.API "EmailSettings:DisplayName" "Auth"
dotnet user-secrets set --project Auth.API "EmailSettings:Password" "<gmail-app-password>"
dotnet user-secrets set --project Auth.API "Google:ClientId" "<google-client-id>"
dotnet user-secrets set --project Auth.API "Google:ClientSecret" "<google-client-secret>"
```

Email uses Gmail SMTP (`smtp.gmail.com:587`) with a Gmail **App Password** — never your login password.

### Database

```bash
dotnet ef database update -p Auth.Infrastructure -s Auth.API
```

### Seeding

Roles are created automatically when the seeder runs. To seed a `SuperAdmin`, uncomment the block in `Program.cs` (`DbSeeder.SeedAsync`) and set the `SuperAdmin:Email`, `SuperAdmin:Password`, `SuperAdmin:FirstName`, `SuperAdmin:LastName` user secrets (the seeder has dev fallbacks).

### Run

```bash
dotnet run --project Auth.API
```

OpenAPI (Swagger UI) is available at `/swagger` in development.

---

## 📬 API Surface

| Method | Route | Purpose |
|---|---|---|
| POST | `/api/auth/register` | Register a user (multipart form, optional photo) |
| POST | `/api/auth/confirm-code` | Confirm email with 6-digit code (logs in) |
| POST | `/api/auth/resend-code` | Resend confirmation code |
| POST | `/api/auth/login` | Login (confirmed accounts only) |
| POST | `/api/auth/refresh-token` | Rotate token pair |
| POST | `/api/auth/logout` | Revoke refresh token |
| POST | `/api/auth/register-organization` | Register an organization (multipart form) |
| POST | `/api/auth/forgot-password` | Email a password-reset code |
| POST | `/api/auth/reset-password` | Set new password using reset code |
| GET | `/api/auth/login-google` | Start Google OAuth sign-in |
| GET | `/api/auth/google-response` | Handle Google OAuth callback |

All DTOs are validated with FluentValidation; validation failures and business errors return structured `AuthResponseDto` payloads.

---

## 🗺️ Roadmap

- [ ] SuperAdmin approval workflow for `Pending` organizations
- [ ] Per-user rate-limit partition keys for OTP endpoints
- [ ] Refresh-token family revocation / theft detection
- [ ] Invalidate access tokens on password change

## 📝 Notes for Contributors

- Write migrations only from `Auth.Infrastructure` (`-p Auth.Infrastructure -s Auth.API`).
- **Review the generated migration before applying it** — if it doesn't match intent and was never applied, delete and regenerate rather than stacking fix-up migrations.
- Follow the layering: thin controllers, business rules in Application, persistence in Infrastructure, **zero dependencies in Domain**.
