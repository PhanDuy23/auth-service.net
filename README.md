# Auth Service

A production-ready authentication and authorization service built with **ASP.NET Core** and **ASP.NET Identity**. Supports JWT authentication, refresh tokens, two-factor authentication (TOTP), OAuth via GitHub and Google, role-based and claims-based authorization.

---

## Features

- **Registration** — email confirmation, role assignment (Customer / Employee / Admin), rollback on failure
- **Login** — lockout protection, brute-force mitigation, Remember Me
- **JWT** — Access Token (30 min) + Refresh Token (24 h) stored in HttpOnly cookie
- **2FA (TOTP)** — RFC 6238, compatible with Google Authenticator / Microsoft Authenticator, 8 recovery codes
- **OAuth** — GitHub OAuth 2.0 and Google OAuth 2.0 (auto account linking)
- **Password management** — forgot password, reset, change password with email notifications
- **Authorization** — Role-based, Claims-based, Policy-based, Resource-based (`SameUserRequirement`)
- **User profile** — view and update profile (FullName, Avatar, DateOfBirth, Department)
- **Admin panel** — paginated user list, search/filter, lock/unlock accounts, soft delete
- **Swagger UI** — full API documentation with JWT auth support

---

## Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core 10 |
| ORM | Entity Framework Core 10 + Npgsql |
| Database | PostgreSQL 16 |
| Auth | ASP.NET Identity + JWT Bearer |
| OAuth | Google OAuth 2.0, GitHub OAuth 2.0 |
| 2FA | Otp.NET (TOTP / RFC 6238) |
| Email | MailKit (SMTP) |
| API Docs | Swashbuckle / Swagger |
| Container | Docker + Docker Compose |
| Config | DotNetEnv (`.env` file) |

---

## Project Structure

```
auth-service/
├── Controllers/
│   ├── AuthController.cs          # Login, logout, refresh token, confirm email
│   ├── RegistrationController.cs  # Register
│   ├── TwoFactorController.cs     # 2FA setup, enable, verify, recovery codes
│   ├── PasswordController.cs      # Forgot, reset, change password
│   ├── UserController.cs          # Profile, user list
│   ├── AdminController.cs         # Admin: manage users, lock/unlock
│   ├── GitHubAuthController.cs    # GitHub OAuth callback
│   └── GoogleAuthController.cs    # Google OAuth callback
│
├── Services/
│   ├── AuthService.cs             # Login, logout, refresh token
│   ├── RegistrationService.cs     # Register, confirm email
│   ├── TwoFactorService.cs        # TOTP setup, verify
│   ├── RecoveryCodeService.cs     # Generate, verify recovery codes
│   ├── PasswordService.cs         # Forgot/reset/change password
│   ├── TokenService.cs            # Generate JWT, refresh token
│   ├── CookieService.cs           # HttpOnly cookie helpers
│   ├── EmailService.cs            # SMTP email sender
│   ├── UserService.cs             # Profile, user list
│   ├── AdminService.cs            # Admin operations
│   ├── GitHubAuthService.cs       # GitHub OAuth flow
│   └── GoogleAuthService.cs       # Google OAuth flow
│
├── Models/
│   ├── ApplicationUser.cs         # Extended IdentityUser
│   ├── RefreshToken.cs
│   └── RecoveryCode.cs
│
├── Authorization/
│   ├── Permissions.cs             # Permission constants (users.read, users.delete, profile.edit)
│   └── SameUserRequirement.cs     # Resource-based: only edit own profile
│
├── Dtos/
│   ├── Requests/                  # LoginDto, RegisterDto, UpdateProfileDto, ...
│   └── Responses/                 # AuthResponseDto, UserProfileDto, PagedResult, ...
│
├── Extensions/                    # Service registration extensions
├── Data/
│   └── ApplicationDbContext.cs
├── Migrations/
├── html/                          # Static demo pages (login, register, report)
├── docker-compose.yml
├── Dockerfile
├── appsettings.json
└── .env.example
```

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (for PostgreSQL)
- A Gmail account with App Password enabled (for email features)

### 1. Clone the repository

```bash
git clone https://github.com/PhanDuy23/auth-service.net.git
cd auth-service.net
```

### 2. Configure environment

```bash
cp .env.example .env
```

Open `.env` and fill in the required values:

```env
POSTGRES_PASSWORD=your_password

JwtSettings__SecretKey=your-secret-key-at-least-32-characters

EmailSettings__SenderEmail=yourmail@gmail.com
EmailSettings__Username=yourmail@gmail.com
EmailSettings__Password=your-gmail-app-password

GoogleOAuth__ClientId=your-google-client-id
GoogleOAuth__ClientSecret=your-google-client-secret

GitHubOAuth__ClientId=your-github-client-id
GitHubOAuth__ClientSecret=your-github-client-secret
```

> **Note:** Google and GitHub OAuth are optional. The service works without them.

### 3. Start with Docker Compose

```bash
docker compose up -d
```

This starts PostgreSQL and the API. The API will be available at `http://localhost:5000`.

### 4. Run database migrations

```bash
dotnet ef database update
```

Or if running inside Docker, the app applies migrations automatically on startup.

### 5. Access Swagger UI

```
http://localhost:5000/swagger
```

---

## Running Locally (without Docker)

### Start PostgreSQL only

```bash
docker compose up postgres -d
```

### Run the API

```bash
dotnet run
```

API runs at `http://localhost:5000` by default (configured in `appsettings.Development.json`).

---

## API Overview

| Method | Endpoint | Description | Auth |
|---|---|---|---|
| POST | `/api/auth/register` | Register new account | — |
| POST | `/api/auth/confirm-email` | Confirm email address | — |
| POST | `/api/auth/login` | Login | — |
| POST | `/api/auth/logout` | Logout | ✓ |
| POST | `/api/auth/refresh-access-token` | Refresh JWT | Cookie |
| GET | `/api/auth/2fa/setup` | Get 2FA QR code | ✓ |
| POST | `/api/auth/2fa/enable` | Enable 2FA | ✓ |
| POST | `/api/auth/2fa/verify` | Verify TOTP code | Cookie |
| POST | `/api/auth/2fa/verify-recovery` | Verify recovery code | Cookie |
| POST | `/api/auth/forgot-password` | Request password reset | — |
| POST | `/api/auth/reset-password` | Reset password | — |
| POST | `/api/auth/change-password` | Change password | ✓ |
| GET | `/api/me` | Get own profile | ✓ |
| PUT | `/api/users/{id}/profile` | Update profile | ✓ Own |
| GET | `/api/users` | List all users | Admin/Employee |
| GET | `/api/admin/users` | Paginated user list | Admin |
| PATCH | `/api/admin/users/{id}/lock` | Lock user | Admin |
| PATCH | `/api/admin/users/{id}/unlock` | Unlock user | Admin |
| GET | `/api/auth/github` | GitHub OAuth login | — |
| GET | `/api/auth/google` | Google OAuth login | — |

---

## Environment Variables Reference

| Variable | Description |
|---|---|
| `POSTGRES_DB` | Database name |
| `POSTGRES_USER` | Database user |
| `POSTGRES_PASSWORD` | Database password (**required**) |
| `JwtSettings__SecretKey` | JWT signing key (min 32 chars) |
| `JwtSettings__ExpiryMinutes` | Access token lifetime (default: 30) |
| `JwtSettings__RefreshTokenExpiryHours` | Refresh token lifetime (default: 24) |
| `EmailSettings__SmtpServer` | SMTP server (default: smtp.gmail.com) |
| `EmailSettings__Port` | SMTP port (default: 587) |
| `EmailSettings__SenderEmail` | From address |
| `EmailSettings__Username` | SMTP username |
| `EmailSettings__Password` | SMTP password / App Password |
| `GoogleOAuth__ClientId` | Google OAuth client ID |
| `GoogleOAuth__ClientSecret` | Google OAuth client secret |
| `GitHubOAuth__ClientId` | GitHub OAuth app client ID |
| `GitHubOAuth__ClientSecret` | GitHub OAuth app client secret |
| `AppSettings__BaseUrl` | API base URL (used in email links) |
| `AppSettings__FrontendUrl` | Frontend URL (used in CORS and redirects) |
