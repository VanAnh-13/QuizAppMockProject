# Quizapp — Backend

ASP.NET Core 10 backend for the Quizapp quiz-management platform. Built with **Clean Architecture**, **Entity Framework Core 10**, **SQL Server**, and **FluentValidation**.

## Project Status

The backend currently provides:

- Domain models for users, roles, quizzes, a reusable question bank, answer options, quiz attempts, and user responses.
- DTOs for authentication, user and role management, quiz management, quiz taking, and attempt history.
- DTOs initialized directly with object initializers; see [DTO construction](docs/dto-construction.md).
- FluentValidation validators registered through dependency injection.
- Role and question creation live in their services; see [creation patterns](docs/creation-patterns.md).
- SQL Server entity mappings and EF Core migrations.
- A development API host with OpenAPI and Swagger UI.
- A read-only public quiz catalog endpoint, `GET /api/public/quizzes`, for active quiz metadata without answers or grading keys.
- A development CORS policy for the Angular client at `http://localhost:4200`.
- xUnit tests for contracts, validation, layer dependencies, dependency injection, database mappings, and persistence.

Controllers and application services implement authentication, user/role management, quiz management, quiz taking, and attempt history. JWT authentication validates each user's security stamp. The API rejects invalid JWT configuration during startup.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- A SQL Server instance for business endpoints, database migrations, and persistence tests (database-independent tests do not need one)
- [EF Core CLI **10.0.11**](https://learn.microsoft.com/en-us/ef/core/cli/dotnet) for migration commands
- Docker with Docker Compose for the full-stack container setup (optional for local .NET development)

## Project Structure

```
backend/
├── Quizapp.sln
├── Quizapp/
│   ├── Quizapp.Domain/           # Entities, enums, shared field limits
│   ├── Quizapp.Application/      # DTOs, FluentValidation validators, DI registration
│   ├── Quizapp.Infrastructure/   # EF Core DbContext, SQL Server config, migrations
│   └── Quizapp.Api/              # ASP.NET Core host, composition root, Swagger
└── tests/
    └── Quizapp.Tests/            # xUnit: contracts, validation, architecture, persistence
```

### Dependency Flow

```
Domain (no deps) → Application (Domain) → Infrastructure (Application + Domain) → Api (Application + Infrastructure)
```

| Project                    | Responsibility                                                                | Key Dependencies                     |
| -------------------------- | ----------------------------------------------------------------------------- | ------------------------------------ |
| **Quizapp.Domain**         | Entities, enums, and shared field limits                                      | None                                 |
| **Quizapp.Application**    | DTOs, request validators, and validator registration                          | FluentValidation 12.1.1              |
| **Quizapp.Infrastructure** | `QuizAppDbContext`, SQL Server configuration, entity mappings, and migrations | EF Core 10.0.11, SQL Server provider |
| **Quizapp.Api**            | ASP.NET Core entry point, DI composition, and development API docs            | JWT Bearer, OpenApi, Swagger UI      |
| **Quizapp.Tests**          | Contract, validation, architecture, model, migration, and persistence tests   | xUnit 2.9.3                          |

## Quick Start

All commands use **PowerShell**. Set the paths once — adjust the root for your checkout:

```powershell
$Backend  = 'D:/Homeworks/c#/Quizapp/backend'
$Solution = "$Backend/Quizapp.sln"
$Api      = "$Backend/Quizapp/Quizapp.Api/Quizapp.Api.csproj"
$Infra    = "$Backend/Quizapp/Quizapp.Infrastructure/Quizapp.Infrastructure.csproj"
$Tests    = "$Backend/tests/Quizapp.Tests/Quizapp.Tests.csproj"
```

### 1. Restore and Build

```powershell
dotnet restore "$Solution"
dotnet build "$Solution" --no-restore
```

### 2. Configure SQL Server and JWT

The API reads `ConnectionStrings:DefaultConnection`. Override the development setting with an environment variable — **never commit credentials**.

```powershell
$env:ConnectionStrings__DefaultConnection = 'Server=localhost;Database=Quizapp;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
```

Adjust server name, instance, and authentication for your environment. `TrustServerCertificate=True` is a local-development convenience only.

Configure JWT before starting the API or running EF tooling with the API startup project:

```powershell
$env:Jwt__Issuer = 'quizapp-local'
$env:Jwt__Audience = 'quizapp-local-client'
$env:Jwt__SigningKey = [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
$env:Jwt__LifetimeMinutes = '60'
```

Issuer and audience must be nonblank, the signing key must contain at least 32 UTF-8 bytes, and the lifetime must be 1–1440 minutes. These values are validated before the host is built and the same settings are used to issue and validate tokens. Restart the API after changing them. Keep signing keys in environment variables or a secret store; generating a new key invalidates tokens signed with the previous key.

### 3. Apply Database Migrations

Install the EF CLI if needed:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.11
```

Apply migrations:

```powershell
dotnet ef database update --project "$Infra" --startup-project "$Api"
```

Compose applies migrations through a separate initialization service before starting the API. Outside Compose, apply migrations explicitly with the command above; Development startup also applies them when sample data is enabled.

### 4. Start the API Host

```powershell
# HTTPS (recommended for development)
dotnet dev-certs https --trust
dotnet run --project "$Api" --launch-profile https

# HTTP only
dotnet run --project "$Api" --launch-profile http
```

| Resource         | URL                                    |
| ---------------- | -------------------------------------- |
| Swagger UI       | https://localhost:7267/swagger         |
| OpenAPI document | https://localhost:7267/openapi/v1.json |
| HTTP listener    | http://localhost:5269                  |

OpenAPI and Swagger UI are exposed **only in Development**. A `404` at `/` is expected — no root endpoint is mapped.

## Password Recovery

Password recovery uses `POST /api/auth/forgot-password` and `POST /api/auth/reset-password`.
Apply the `AddPasswordResetTokens` migration and configure the frontend reset URL and SMTP before use.
See [password reset setup and contract](docs/password-reset.md).

## Quiz Attempt Contract

Quiz attempts persist their owner, deadline, saved answers, pause state and a snapshot of the starting quiz. Pausing freezes the remaining time; resuming continues the same attempt. The frontend runs the timer and calls submission; there is no backend submission scheduler.

- `POST /api/quizzes/{quizId}/start` creates an attempt and returns its ID, revision and UTC timestamps.
- `GET /api/attempts/in-progress` lists the current user's unfinished attempts.
- `GET` / `PUT /api/attempts/{attemptId}/progress` restore or save the full answer list.
- `POST /api/attempts/{attemptId}/pause` saves answers and pauses atomically; `/resume` continues with the remaining time.
- `POST /api/attempts/{attemptId}/submit` grades the saved answers. At or after expiry, submissions can only grade answers saved before the deadline.
- `GET /api/attempts/{attemptId}` and `GET /api/quiz-history` return submitted results.

Apply the `AddAttemptPauseAndDraft` migration before using these endpoints. See [the full attempt-progress contract](docs/attempt-progress.md) for DTOs, frontend sequencing, deadline boundaries, conflicts, legacy compatibility and migration behavior.

## Data Model

- **Users** have profiles, an active/deactivated status, and role assignments through `UserRole`.
- **Quizzes** reuse questions through `QuizQuestion` — questions are not duplicated per quiz.
- **Questions** have answer options, an Easy/Medium/Hard level, and one of six types: MultipleChoice, TrueFalse, SingleChoice, FillInTheBlanks, ShortAnswer, or LongAnswer.
- **Quiz attempts** and user answers model attempt history, selected options, and text responses.
- **Validation** covers nested profiles, supported enum values, duplicate selections, mutually exclusive option/text responses, and finite passing-score percentages in the range 0–100 (comparable to the percentage-based score returned by submission).

## Testing

### Run All Tests

```powershell
dotnet test "$Tests"
```

Without `QUIZAPP_TEST_SQLSERVER_CONNECTION_STRING`, tests requiring a live SQL Server are skipped. Contract, validation, architecture, and EF model tests still run.

### SQL Server Integration Tests

Use a **dedicated development/test** SQL Server instance with an account allowed to create and drop databases:

```powershell
$env:QUIZAPP_TEST_SQLSERVER_CONNECTION_STRING = 'Server=localhost;Database=master;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
dotnet test "$Tests"
Remove-Item Env:QUIZAPP_TEST_SQLSERVER_CONNECTION_STRING
```

The fixture creates a unique `QuizappRelationTests_<guid>` database, applies migrations, and drops it during cleanup. **Never point these tests at a production instance.** Interrupted runs may leave temporary databases that require manual cleanup.

### Test Categories

| Directory       | What It Tests                                                  |
| --------------- | -------------------------------------------------------------- |
| `Application/`  | DTO contract shapes, FluentValidation request validation       |
| `Architecture/` | Layer dependency boundaries, DI/service registration           |
| `Api/`          | Real HTTP routing and JWT middleware with test repositories    |
| `Data/`         | EF Core model/schema, SQL Server persistence and relationships |

### Focused Test Run

```powershell
dotnet test "$Tests" --filter 'FullyQualifiedName~RequestValidationTests'
```

## Docker

The repository includes a multi-stage Linux Dockerfile (`Quizapp/Quizapp.Api/Dockerfile`) and a full-stack Compose configuration. Install Docker with Compose, start Docker, and run from the repository root:

```powershell
Copy-Item '.env.example' '.env'
```

Keep an existing `.env` when updating your checkout. Before starting, edit `.env` to replace `SA_PASSWORD` with a valid SQL Server administrator password and `JWT_SIGNING_KEY` with a random signing key of at least 32 bytes. The template includes a PowerShell command to generate the signing key.

```powershell
docker compose up -d --build
```

Compose supplies the API and migration service with a SQL Server connection string targeting `db:1433`, using `DB_NAME` and `SA_PASSWORD` from `.env`. It defaults to `ASPNETCORE_ENVIRONMENT=Development` and `SAMPLE_DATA_ENABLED=false`. Sample data requires explicit opt-in and is seeded only in Development. The published host ports default to 1433 for SQL Server, 5269 for the API, and 4200 for the frontend, and can be changed with `DB_PORT`, `API_PORT`, and `WEB_PORT`. All three bind to `127.0.0.1` (localhost) for local development.

To opt in to sample quizzes and demo accounts, set `SAMPLE_DATA_ENABLED=true` in `.env` and rerun `docker compose up -d --build`. This creates `demo_user` and `demo_admin` with a publicly known password; keep this setup local. Setting the flag back to `false` prevents further seeding but does not remove existing accounts from the persistent volume. For an existing setup, set the flag to `false` and rerun the startup command to apply the localhost bindings; disable or change the credentials of any existing demo accounts before exposing the application beyond localhost.

Startup proceeds in this order:

1. `db` starts SQL Server Developer edition and passes its health check.
2. `quizapp-migration` runs the API image with `--migrate`, applies pending EF Core migrations to the configured database, and exits.
3. `quizapp-api` starts only after migration exits successfully, then seeds sample data when enabled in Development.
4. `quizapp-web` starts the Angular frontend served by nginx.

The migration service runs even when `SAMPLE_DATA_ENABLED=false`. A fresh `sqlserver-data` volume therefore gets its schema before any API requests. Repeated migration runs preserve existing data and apply only pending migrations. If migration fails, Compose blocks API startup; inspect `docker compose logs quizapp-migration`, correct the problem, and rerun the startup command.

Default URLs are `http://localhost:4200` for the frontend and `http://localhost:5269` for the API. Ports, database name, environment and credentials are configured in `.env`. The SQL Server volume persists across container restarts. HTTPS certificates and TLS termination require separate configuration.

## Useful Commands

| Command                                                                 | Purpose                                      |
| ----------------------------------------------------------------------- | -------------------------------------------- |
| `dotnet restore "$Solution"`                                            | Restore NuGet dependencies                   |
| `dotnet build "$Solution" -c Release`                                   | Build all projects in Release mode           |
| `dotnet test "$Tests"`                                                  | Run tests (SQL Server tests are conditional) |
| `dotnet run --project "$Api" --launch-profile https`                    | Run the development host with HTTPS          |
| `dotnet ef dbcontext info --project "$Infra" --startup-project "$Api"`  | Inspect the EF context and provider          |
| `dotnet ef database update --project "$Infra" --startup-project "$Api"` | Apply migrations                             |

### Creating a New Migration

After an intentional model change:

```powershell
dotnet ef migrations add DescribeYourChange --project "$Infra" --startup-project "$Api" --output-dir Persistence/Migrations
```

Review the generated schema changes before applying.

## Troubleshooting

| Problem                                               | Solution                                                                                                                          |
| ----------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------- |
| Swagger is missing                                    | Use a Development launch profile and navigate to `/swagger`, not `/`                                                              |
| API fails before startup with JWT configuration error | Check `Jwt__Issuer`, `Jwt__Audience`, `Jwt__SigningKey`, and `Jwt__LifetimeMinutes`                                               |
| SQL Server connection fails                           | Check server name, credentials, network access, certificate settings, and the `ConnectionStrings__DefaultConnection` env variable |
| Database tables missing                               | For Compose, inspect `docker compose logs quizapp-migration`; for local setup, run `dotnet ef database update`                                                          |
| Persistence tests skipped                             | Set `QUIZAPP_TEST_SQLSERVER_CONNECTION_STRING` for a dedicated test instance                                                      |
| HTTPS certificate errors                              | Trust the .NET dev certificate (`dotnet dev-certs https --trust`) or use the HTTP launch profile                                  |
