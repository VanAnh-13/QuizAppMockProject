# Quizapp — Backend

ASP.NET Core 10 backend for the Quizapp quiz-management platform. Built with **Clean Architecture**, **Entity Framework Core 10**, **SQL Server**, and **FluentValidation**.

## Project Status

The backend currently provides:

- Domain models for users, roles, quizzes, a reusable question bank, answer options, quiz attempts, and user responses.
- DTOs for authentication, user and role management, quiz management, quiz taking, and attempt history.
- Nested builders in all DTOs, using `new CreateQuizDto.Builder().WithTitle(...).Build()`; see [DTO builder examples](docs/dto-builders.md).
- FluentValidation validators registered through dependency injection.
- Role and question factories, with creation strategies for all six question types; see [creation patterns](docs/creation-patterns.md).
- SQL Server entity mappings and EF Core migrations.
- A development API host with OpenAPI and Swagger UI.
- xUnit tests for contracts, validation, layer dependencies, dependency injection, database mappings, and persistence.

Controllers and application services implement authentication, user/role management, quiz management, quiz taking, and attempt history. JWT authentication validates each user's security stamp. The API rejects invalid JWT configuration during startup.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- A SQL Server instance for business endpoints, database migrations, and persistence tests (database-independent tests do not need one)
- [EF Core CLI **10.0.11**](https://learn.microsoft.com/en-us/ef/core/cli/dotnet) for migration commands
- Optional: Docker with Docker Compose for building the container image

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

| Project | Responsibility | Key Dependencies |
| --- | --- | --- |
| **Quizapp.Domain** | Entities, enums, and shared field limits | None |
| **Quizapp.Application** | DTOs, request validators, and validator registration | FluentValidation 12.1.1 |
| **Quizapp.Infrastructure** | `QuizAppDbContext`, SQL Server configuration, entity mappings, and migrations | EF Core 10.0.11, SQL Server provider |
| **Quizapp.Api** | ASP.NET Core entry point, DI composition, and development API docs | JWT Bearer, OpenApi, Swagger UI |
| **Quizapp.Tests** | Contract, validation, architecture, model, migration, and persistence tests | xUnit 2.9.3 |

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

The API does **not** automatically apply migrations at startup.

### 4. Start the API Host

```powershell
# HTTPS (recommended for development)
dotnet dev-certs https --trust
dotnet run --project "$Api" --launch-profile https

# HTTP only
dotnet run --project "$Api" --launch-profile http
```

| Resource | URL |
| --- | --- |
| Swagger UI | https://localhost:7267/swagger |
| OpenAPI document | https://localhost:7267/openapi/v1.json |
| HTTP listener | http://localhost:5269 |

OpenAPI and Swagger UI are exposed **only in Development**. A `404` at `/` is expected — no root endpoint is mapped.

## Quiz Attempt Contract

Send a valid bearer token with these requests:

1. `POST /api/quizzes/{quizId}/start` creates and saves an attempt owned by the current user. The response contains `attemptId`, `startedAt`, `expiresAt`, and the quiz questions without correct-answer flags.
2. `POST /api/quizzes/{quizId}/submit` requires that `attemptId` in the request body, alongside `answers`. It completes the saved attempt and returns the same ID.
3. `GET /api/attempts/{attemptId}` and `GET /api/quiz-history` return submitted results. An in-progress attempt has no result and is excluded from history.

Example submission body:

```json
{
  "attemptId": "<attemptId returned by start>",
  "answers": []
}
```

An empty answer list submits an unanswered quiz. The attempt must belong to the authenticated user and the quiz in the route. Submission at or after the saved expiry returns `400`; changing the quiz duration later does not extend an existing attempt. A missing/empty attempt ID returns `422`, an unknown ID returns `404`, another user's attempt returns `403`, and an already submitted attempt returns `409`. The submission timestamp is a concurrency token to prevent a stale submission from overwriting a completed result.

**Client compatibility:** start previously used `GET`; clients must now use `POST` and retain the returned ID for submission. Submitting without starting is no longer supported.

For single-choice and true/false questions, a submitted response must select exactly one active answer belonging to that question. Invalid selections return `422` without completing the attempt, so the client can correct and resubmit. Omit a question from `answers` to leave it unanswered (zero points). Multiple-choice questions continue to allow multiple selections.

Apply the `PersistQuizAttemptLifecycle` migration before using this flow. It adds start/expiry timestamps and makes the submission timestamp nullable. Existing submitted scores and timestamps are preserved; because their start times were never recorded, their submission timestamps are used for the legacy start/expiry values. Rolling back this migration is blocked while unsubmitted attempts exist.

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

| Directory | What It Tests |
| --- | --- |
| `Application/` | DTO contract shapes, FluentValidation request validation |
| `Architecture/` | Layer dependency boundaries, DI/service registration |
| `Api/` | Real HTTP routing and JWT middleware with test repositories |
| `Data/` | EF Core model/schema, SQL Server persistence and relationships |

### Focused Test Run

```powershell
dotnet test "$Tests" --filter 'FullyQualifiedName~RequestValidationTests'
```

## Docker

The repository includes a multi-stage Linux Dockerfile (`Quizapp/Quizapp.Api/Dockerfile`). Build the API image using the supplied Compose configuration:

```powershell
docker compose -f "D:/Homeworks/c#/Quizapp/compose.yaml" build
```

The Compose configuration currently defines **only the API image/build**. It does not configure:

- A SQL Server service or connection string override
- Published host ports
- The Development environment required for Swagger
- HTTPS certificate or TLS termination

Configure those explicitly before using Compose as a runnable stack.

## Useful Commands

| Command | Purpose |
| --- | --- |
| `dotnet restore "$Solution"` | Restore NuGet dependencies |
| `dotnet build "$Solution" -c Release` | Build all projects in Release mode |
| `dotnet test "$Tests"` | Run tests (SQL Server tests are conditional) |
| `dotnet run --project "$Api" --launch-profile https` | Run the development host with HTTPS |
| `dotnet ef dbcontext info --project "$Infra" --startup-project "$Api"` | Inspect the EF context and provider |
| `dotnet ef database update --project "$Infra" --startup-project "$Api"` | Apply migrations |

### Creating a New Migration

After an intentional model change:

```powershell
dotnet ef migrations add DescribeYourChange --project "$Infra" --startup-project "$Api" --output-dir Persistence/Migrations
```

Review the generated schema changes before applying.

## Troubleshooting

| Problem | Solution |
| --- | --- |
| Swagger is missing | Use a Development launch profile and navigate to `/swagger`, not `/` |
| API fails before startup with JWT configuration error | Check `Jwt__Issuer`, `Jwt__Audience`, `Jwt__SigningKey`, and `Jwt__LifetimeMinutes` |
| SQL Server connection fails | Check server name, credentials, network access, certificate settings, and the `ConnectionStrings__DefaultConnection` env variable |
| Database tables missing | Run `dotnet ef database update` — startup does not migrate automatically |
| Persistence tests skipped | Set `QUIZAPP_TEST_SQLSERVER_CONNECTION_STRING` for a dedicated test instance |
| HTTPS certificate errors | Trust the .NET dev certificate (`dotnet dev-certs https --trust`) or use the HTTP launch profile |
