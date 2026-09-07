# Quizapp — Backend

ASP.NET Core 10 backend for the Quizapp quiz-management platform. Built with **Clean Architecture**, **Entity Framework Core 10**, **SQL Server**, and **FluentValidation**.

## Project Status

The backend currently provides:

- Domain models for users, roles, quizzes, a reusable question bank, answer options, quiz attempts, and user responses.
- DTOs for authentication, user and role management, quiz management, quiz taking, and attempt history.
- Nested builders in all DTOs, using `new CreateQuizDto.Builder().WithTitle(...).Build()`; see [DTO builder examples](docs/dto-builders.md).
- FluentValidation validators registered through dependency injection.
- SQL Server entity mappings and EF Core migrations.
- A development API host with OpenAPI and Swagger UI.
- xUnit tests for contracts, validation, layer dependencies, dependency injection, database mappings, and persistence.

**Business endpoints are not implemented yet.** There are no controllers or mapped quiz/authentication routes and no configured JWT authentication pipeline. The JWT bearer package and authentication DTOs are present but do not provide a working login flow.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- A SQL Server instance for database migrations and persistence tests (database-independent tests and the Swagger-only host can run without one)
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

### 2. Configure SQL Server

The API reads `ConnectionStrings:DefaultConnection`. Override the development setting with an environment variable — **never commit credentials**.

```powershell
$env:ConnectionStrings__DefaultConnection = 'Server=localhost;Database=Quizapp;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
```

Adjust server name, instance, and authentication for your environment. `TrustServerCertificate=True` is a local-development convenience only.

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

## Data Model

- **Users** have profiles, an active/deactivated status, and role assignments through `UserRole`.
- **Quizzes** reuse questions through `QuizQuestion` — questions are not duplicated per quiz.
- **Questions** have answer options, an Easy/Medium/Hard level, and one of six types: MultipleChoice, TrueFalse, SingleChoice, FillInTheBlanks, ShortAnswer, or LongAnswer.
- **Quiz attempts** and user answers model attempt history, selected options, and text responses.
- **Validation** covers nested profiles, supported enum values, duplicate selections, mutually exclusive option/text responses, and finite non-negative passing scores (absolute values, not percentages).

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
| Swagger has no operations | Expected at this stage — business endpoints are not implemented |
| SQL Server connection fails | Check server name, credentials, network access, certificate settings, and the `ConnectionStrings__DefaultConnection` env variable |
| Database tables missing | Run `dotnet ef database update` — startup does not migrate automatically |
| Persistence tests skipped | Set `QUIZAPP_TEST_SQLSERVER_CONNECTION_STRING` for a dedicated test instance |
| HTTPS certificate errors | Trust the .NET dev certificate (`dotnet dev-certs https --trust`) or use the HTTP launch profile |
