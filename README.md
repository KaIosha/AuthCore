# ASP.NET Core Auth API

Implement a complete, secure auth flow: user and organization registration, email confirmation with OTP codes, JWT + rotating refresh tokens, password recovery, and Google OAuth sign-in.
---

## What This Project Does

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

## 🧱 Architecture

**Clean Architecture** with a strict dependency direction — **API → Application → Domain**, Infrastructure implements the contracts:

```
Auth.API/             Presentation. Controllers, rate limiting, config.
Auth.Application/     Use cases, DTOs, services, validators.
Auth.Domain/          Entities, enums, roles, domain interfaces.
Auth.Infrastructure/  EF Core, migrations, DbContext, repositories, seeding.
```

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

Roles are created automatically when the seeder runs. To seed a `SuperAdmin`, uncomment the block in `Program.cs` (`DbSeeder.SeedAsync`) and set the `SuperAdmin:Email`, `SuperAdmin:Password`, `SuperAdmin:FirstName`, `SuperAdmin:LastName` user secrets (the seeder has dev fallbacks). ** I put my Email for Test

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
