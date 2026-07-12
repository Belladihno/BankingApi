# Account & Transaction Banking API

A production-grade RESTful banking simulation built with **ASP.NET Core 8**, **Entity Framework Core 8**, and **SQL Server**, following Clean Architecture principles. Integrates with **Paystack** for payment processing.

## Architecture

```
BankingApi.Domain          — Pure domain layer (no NuGet dependencies)
    → BankingApi.Application  — DTOs, interfaces, services, validators
        → BankingApi.Infrastructure — EF Core, Identity, JWT, Paystack, repos
            → BankingApi.Api          — Controllers, middleware, Program.cs
```

| Layer | Responsibility |
|---|---|
| **Domain** | Entities (`Account`, `ApplicationUser`, `Transaction`, `Payment`), enums, exceptions |
| **Application** | Use-case services (`AuthService`, `AccountService`, `TransactionService`), DTOs, FluentValidation validators, repository interfaces |
| **Infrastructure** | EF Core DbContext + migrations, repository implementations, JWT/BCrypt/Paystack services, background jobs |
| **Api** | Controllers, global exception middleware, Swagger, JWT auth config, Serilog, rate limiting |

## Tech Stack

| Component | Choice |
|---|---|
| Framework | ASP.NET Core 8 (Web API) |
| ORM | Entity Framework Core 8 (Code-First, Fluent API) |
| Database | SQL Server (local: `Server=.;Database=BankingApiDb`) |
| Authentication | JWT Bearer (HMAC-SHA256, 15-minute expiry) |
| Password Hashing | BCrypt (work factor 12) via `BCrypt.Net-Next` |
| Payment Gateway | Paystack (test mode) |
| Logging | Serilog (Console + rolling file) |
| Validation | FluentValidation 12 |
| API Docs | Swagger UI (Swashbuckle) |
| Rate Limiting | ASP.NET Core Rate Limiter (login: 10 req/min) |
| Testing | xUnit + Moq + FluentAssertions |

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- SQL Server (local instance: `Server=.`)
- (Optional) [Paystack test keys](https://dashboard.paystack.com/#/settings/developer) for payment integration

## Getting Started

### 1. Clone & Configure

```bash
git clone <repo-url>
cd BankingApi
```

### 2. Set Connection String

Open `BankingApi.Api/appsettings.Development.json` — the default connection string uses Windows Authentication:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=BankingApiDb;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

For SQL Authentication, use:
```json
"Server=.;Database=BankingApiDb;User Id=sa;Password=your_password;TrustServerCertificate=True;"
```

### 3. Apply Migrations

```bash
dotnet ef database update --project BankingApi.Infrastructure --startup-project BankingApi.Api
```

This creates the database and runs all 5 migrations (InitialCreate, AddRoleToApplicationUser, FixTransactionInitiatorFk, AddPaymentsTable, AddLastDailyResetDate).

### 4. Run

```bash
dotnet run --project BankingApi.Api
```

The server starts at `https://localhost:7081`. Swagger UI is available at `https://localhost:7081/swagger`.

### 5. Run Tests

```bash
dotnet test
```

20 unit tests covering AuthService (5), AccountService (6), TransactionService (9).

## Seeded Test Data

On every development startup, `DataSeeder` creates:

### Users

| Email | Password | Role |
|---|---|---|
| `admin@bankingapi.local` | `Admin@12345!` | Admin |
| `teller@bankingapi.local` | `Teller@12345!` | Teller |
| `customer@bankingapi.local` | `Customer@12345!` | Customer |

### Accounts (owned by Customer)

| Account Number | Type | Balance | Daily Limit |
|---|---|---|---|
| `0580000001` | Savings | 10,000.00 | 200,000.00 |
| `0580000002` | Current | 50,000.00 | 500,000.00 |

## API Endpoints

### Authentication — `api/v1/auth`

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `register` | — | Register a new customer |
| POST | `login` | — | Login, returns JWT + refresh token |
| POST | `refresh-token` | — | Refresh access token |
| POST | `setup-pin` | Bearer | Set 4-digit transaction PIN (one-time) |
| POST | `change-pin` | Bearer | Change transaction PIN |
| POST | `logout` | Bearer | Revoke refresh token |

### Accounts — `api/v1/accounts`

| Method | Endpoint | Role | Description |
|---|---|---|---|
| POST | `/` | Teller, Admin | Open a new account |
| GET | `/` | Admin | List all accounts (paginated, filterable) |
| GET | `my-accounts` | Customer, Teller, Admin | Get current user's accounts |
| GET | `{accountId}` | Any | Get account by ID |
| GET | `by-number/{accountNumber}` | Teller, Admin | Get account by number |
| PATCH | `{accountId}/status` | Admin | Update account status |
| PATCH | `{accountId}/daily-limit` | Admin | Update daily withdrawal limit |

### Transactions — `api/v1/transactions`

| Method | Endpoint | Role | Description |
|---|---|---|---|
| POST | `deposit` | Teller, Admin | Deposit funds (no PIN required) |
| POST | `withdraw` | Customer | Withdraw funds (PIN required) |
| POST | `transfer` | Customer | Transfer between accounts (PIN required) |
| GET | `{transactionId}` | Any | Get transaction details |
| GET | `account/{accountId}` | Any | Get account transaction history |
| POST | `{transactionId}/reverse` | Admin | Reverse a completed transaction |

### Payments (Paystack) — `api/v1/payments`

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `initialize` | Customer | Initialize a Paystack deposit |
| POST | `webhook` | Anonymous | Paystack webhook receiver (HMAC-verified) |

### Users — `api/v1/users`

| Method | Endpoint | Role | Description |
|---|---|---|---|
| GET | `/` | Admin | List all users |
| GET | `{userId}` | Admin | Get user by ID |
| PATCH | `{userId}/deactivate` | Admin | Deactivate user |
| PATCH | `{userId}/activate` | Admin | Activate user |
| POST | `{userId}/reset-pin` | Admin | Reset user's transaction PIN |

### Audit — `api/v1/audit`

| Method | Endpoint | Role | Description |
|---|---|---|---|
| GET | `/` | Admin | List audit logs (filterable, paginated) |
| GET | `{auditLogId}` | Admin | Get audit log by ID |

## Domain Model

### Entities

- **ApplicationUser** — Users with roles (`Customer`, `Teller`, `Admin`), BCrypt password hash, BCrypt transaction PIN hash with 3-failure lockout
- **Account** — Bank accounts (Savings/Current), tracks balance, daily withdrawal limit, `TodayWithdrawnAmount`, `LastDailyResetDate`
- **Transaction** — Double-entry transactions (Deposit/Withdrawal/Transfer) with `TransactionLedgerEntry` records (Debit/Credit)
- **Payment** — Paystack payment records (Pending/Success/Failed)
- **AuditLog** — Immutable audit trail with before/after JSON snapshots

### Enums

| Enum | Values |
|---|---|
| `AccountType` | Savings, Current |
| `AccountStatus` | Active, Dormant, Frozen, Closed |
| `TransactionType` | Deposit, Withdrawal, Transfer |
| `TransactionStatus` | Pending, Completed, Failed, Reversed |
| `LedgerEntryType` | Debit, Credit |
| `PaymentStatus` | Pending, Success, Failed |

### Key Business Rules

- Minimum deposit/transfer: ₦100.00
- Maximum single transaction: ₦5,000,000.00
- Minimum opening deposit: ₦500.00
- Savings daily withdrawal limit: ₦200,000.00
- Current daily withdrawal limit: ₦500,000.00
- Transaction PIN: exactly 4 numeric digits
- PIN lockout after 3 consecutive failures (5-minute cooldown)
- Account prefix: `058` (GTBank code)

## Daily Withdrawal Limit Reset

The `TodayWithdrawnAmount` resets under two mechanisms:

1. **On-demand (primary)**: Every withdrawal/transfer first checks if `LastDailyResetDate` is from a previous day. If so, the counter is reset to 0 before processing the transaction.
2. **Scheduled (housekeeping)**: `DailyLimitResetJob` runs daily at 01:00 UTC as a safety sweep.

This ensures the limit is always correct even if the app was not running at the scheduled reset time.

## Paystack Integration

### Deposit Flow

1. **Initialize**: Client calls `POST /api/v1/payments/initialize` with `accountId`, `amount`, `email`
2. **Save + Call Paystack**: A `Payment` record is saved as `Pending`, then Paystack's `/transaction/initialize` is called
3. **Return URL**: Paystack's `authorization_url` is returned to client for checkout
4. **Webhook**: After checkout, Paystack sends `charge.success` event to `POST /api/v1/payments/webhook`
5. **Verify + Credit**: HMAC-SHA512 signature is verified → idempotency check → amount verification → account credited inside a DB transaction
6. **Ledger**: A deposit `Transaction` + `LedgerEntry` (Credit) is created

### Withdrawal Flow

Same pattern, but targets Paystack's `/transfer` endpoint. The webhook listens for `transfer.success`.

### Webhook Security

- HMAC-SHA512 computed from raw body using `Paystack:SecretKey`
- Only one signature check — no separate webhook secret needed
- Idempotent via `Payment.Status` check (skips if already `Success`)
- Amount verified against the database to prevent tampering

## Exception Handling

All unhandled exceptions are caught by `ExceptionHandlingMiddleware`:

| Exception | HTTP Status | Response |
|---|---|---|
| `DomainException` (and subclasses) | 422 Unprocessable Entity | RFC 7807 ProblemDetails with error code |
| All other exceptions | 500 Internal Server Error | Generic error (no stack trace leaked) |

Custom domain exceptions: `AccountClosedException`, `AccountFrozenException`, `InsufficientFundsException`, `InvalidPinException`, `PinLockedException`.

## Security

- **Password hashing**: BCrypt (work factor 12)
- **PIN hashing**: BCrypt (work factor 12) — no PIN stored in plain text
- **PIN lockout**: 3 consecutive failures → 5-minute lock
- **JWT**: HMAC-SHA256, 15-minute expiry
- **Role-based access**: `[Authorize(Roles = "...")]` on every sensitive endpoint
- **Webhook**: HMAC-SHA512 signature verification
- **Rate limiting**: Login endpoint limited to 10 requests/minute
- **Refresh tokens**: Cryptographic 64-byte random tokens

## Project Structure

```
BankingApi/
├── BankingApi.Domain/           # Entities, enums, exceptions
│   ├── Entities/                # Account, ApplicationUser, AuditLog, Payment, Transaction, TransactionLedgerEntry
│   ├── Enums/                   # AccountType, AccountStatus, LedgerEntryType, PaymentStatus, TransactionStatus, TransactionType, UserRole
│   └── Exceptions/              # DomainException + subclasses
├── BankingApi.Application/      # Use cases, DTOs, interfaces, validators
│   ├── DTOs/                    # Request/response objects (Auth, Accounts, Transactions, Payments, Users)
│   ├── Interfaces/              # Service + repository interfaces
│   ├── Services/                # AuthService, AccountService, TransactionService, UserService, AuditService
│   └── Validators/              # FluentValidation validators
├── BankingApi.Infrastructure/   # EF Core, Identity, repos, external services
│   ├── BackgroundJobs/          # DailyLimitResetJob
│   ├── Data/                    # ApplicationDbContext, DataSeeder, Configurations (EF Fluent API)
│   ├── Migrations/              # 5 EF Core migrations
│   ├── Repositories/            # Account, AuditLog, Payment, Transaction, User repositories
│   └── Services/                # PasswordHasher, TokenService, PaystackService
├── BankingApi.Api/              # API layer
│   ├── Controllers/             # Auth, Accounts, Transactions, Payments, Users, Audit
│   ├── Extensions/              # ServiceCollectionExtensions (Swagger, JWT, CORS, RateLimit)
│   ├── Middleware/              # ExceptionHandlingMiddleware
│   └── Program.cs               # App startup, middleware pipeline, DI
└── BankingApi.Tests/            # Unit tests
    └── Services/                # AuthServiceTests, AccountServiceTests, TransactionServiceTests
```
