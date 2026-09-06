# Quizapp

A quiz-management backend foundation built with **ASP.NET Core 10**, **Entity Framework Core 10**, **SQL Server**, and **FluentValidation**. The solution separates domain models, application contracts and validation, persistence, and the API host.

## Project status

The repository currently provides:

- Domain models for users, roles, quizzes, a reusable question bank, answer options, quiz attempts, and user responses.
- DTOs for authentication, user and role management, quiz management, quiz taking, and attempt history.
- FluentValidation validators registered through dependency injection.
- SQL Server entity mappings and EF Core migrations.
- A development API host with OpenAPI and Swagger UI.
- xUnit tests for contracts, validation, layer dependencies, dependency injection, database mappings, and persistence.

**Business endpoints are not implemented yet.** There are no controllers or mapped quiz/authentication routes, no frontend, and no configured JWT authentication pipeline. The JWT bearer package and authentication DTOs are present, but do not provide a working login flow. Swagger currently has no business operations to display. Validators are registered, but are not yet connected to HTTP request handling.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).
- A SQL Server instance for database migrations and persistence tests. Database-independent tests and the current Swagger-only host can run without a live database.
- EF Core CLI **10.0.11** for migration commands.
- Optional: Docker with Docker Compose for building the container image.

The commands below use **PowerShell**. Set the repository location once; change it if your checkout is elsewhere:

```powershell
$RepoRoot = 'D:/Homeworks/c#/Quizapp'
$Solution = "$RepoRoot/Quizapp.sln"
$Api = "$RepoRoot/Quizapp/Quizapp.Api/Quizapp.Api.csproj"
$Infrastructure = "$RepoRoot/Quizapp/Quizapp.Infrastructure/Quizapp.Infrastructure.csproj"
$Tests = "$RepoRoot/tests/Quizapp.Tests/Quizapp.Tests.csproj"
```

## Quick start

### 1. Restore and build

```powershell
dotnet restore "$Solution"
dotnet build "$Solution" --no-restore
```

Nullable reference types and implicit usings are enabled. The shared build configuration treats compiler/build warnings as errors.

### 2. Configure SQL Server

The API reads `ConnectionStrings:DefaultConnection`. Override the development setting with an environment variable instead of committing credentials.

For a local Windows-authenticated SQL Server instance:

```powershell
$env:ConnectionStrings__DefaultConnection = 'Server=localhost;Database=Quizapp;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
```

Adjust the server/instance name and authentication method for your environment. Windows integrated authentication requires a suitable Windows SQL Server setup; it is not a generic Linux-container configuration.

`TrustServerCertificate=True` is a local-development convenience. Use a trusted server certificate and appropriate secret management outside local development. Do not commit real database credentials.

### 3. Apply database migrations

Install the EF CLI if it is not already available:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.11
```

If it is already installed at a different version, use `dotnet tool update --global dotnet-ef --version 10.0.11`.

With the connection variable set in the same shell, apply the schema to your development database:

```powershell
dotnet ef database update --project "$Infrastructure" --startup-project "$Api"
```

The migrations create the initial schema, add user email, and add profile and quiz/question fields. The API does **not** automatically apply migrations at startup.

### 4. Start the API host

For HTTPS, trust the development certificate and use the HTTPS launch profile:

```powershell
dotnet dev-certs https --trust
dotnet run --project "$Api" --launch-profile https
```

Development URLs:

| Resource | URL |
| --- | --- |
| Swagger UI | https://localhost:7267/swagger |
| OpenAPI document | https://localhost:7267/openapi/v1.json |
| HTTP listener | http://localhost:5269 |

The HTTPS profile enables both listeners; HTTP requests may redirect to HTTPS. Alternatively, start the HTTP-only profile:

```powershell
dotnet run --project "$Api" --launch-profile http
```

For that profile, use `http://localhost:5269/swagger`. HTTPS redirection middleware remains enabled and may log a warning when no HTTPS port is available.

OpenAPI and Swagger UI are exposed **only in Development**. A `404` at `/` is expected because no root endpoint is mapped.

## Architecture

Paths below are relative to the repository root configured above.

| Project / location | Responsibility |
| --- | --- |
| `Quizapp/Quizapp.Domain` | Entities, enums, and shared field limits; no application or infrastructure dependencies. |
| `Quizapp/Quizapp.Application` | DTOs, request validators, and validator registration; depends on Domain. |
| `Quizapp/Quizapp.Infrastructure` | `QuizAppDbContext`, SQL Server configuration, entity mappings, and migrations; references Application and Domain. |
| `Quizapp/Quizapp.Api` | ASP.NET Core entry point, dependency injection composition, and development API documentation; references Application and Infrastructure. |
| `tests/Quizapp.Tests` | xUnit contract, validation, architecture, model, migration, and persistence tests. |

Keep persistence and ASP.NET Core dependencies out of the inner layers. Architecture tests enforce the dependency boundaries, and dependency-injection tests verify scoped validator and database-context registration.

### Data model

- Users have profiles, an active/deactivated status, and role assignments through `UserRole`.
- Quizzes reuse questions through `QuizQuestion` rather than owning separate copies of each question.
- Questions have answer options, an Easy/Medium/Hard level, and one of six types: MultipleChoice, TrueFalse, SingleChoice, FillInTheBlanks, ShortAnswer, or LongAnswer.
- Quiz attempts and user answers model attempt history, selected options, and text responses.
- Validation covers nested profiles, supported enum values, duplicate selections, mutually exclusive option/text responses, and finite non-negative passing scores. Passing scores are absolute values, not percentages.

These are existing model and validation capabilities, not completed end-to-end API workflows.

## Tests

Run the test suite:

```powershell
dotnet test "$Tests"
```

Without `QUIZAPP_TEST_SQLSERVER_CONNECTION_STRING`, tests requiring a live SQL Server instance are skipped. Contract, validation, architecture, and EF model tests still run.

### SQL Server integration tests

Use a dedicated development/test SQL Server instance and an account allowed to create and drop databases:

```powershell
$env:QUIZAPP_TEST_SQLSERVER_CONNECTION_STRING = 'Server=localhost;Database=master;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
dotnet test "$Tests"
Remove-Item Env:QUIZAPP_TEST_SQLSERVER_CONNECTION_STRING
```

The fixture replaces the supplied database name with a unique `QuizappRelationTests_<guid>` name, applies migrations, and drops that temporary database during cleanup. It does not use the database named in the input connection string as the test database. Nevertheless, **never point these tests at a production SQL Server instance**. Interrupted test runs may leave temporary databases that require manual cleanup.

## Useful commands

Run these after setting the PowerShell variables from the Requirements section.

| Command | Purpose |
| --- | --- |
| `dotnet restore "$Solution"` | Restore NuGet dependencies. |
| `dotnet build "$Solution" -c Release` | Build all projects in Release mode. |
| `dotnet test "$Tests"` | Run tests; SQL Server tests are conditional. |
| `dotnet run --project "$Api" --launch-profile https` | Run the development host with HTTPS. |
| `dotnet ef dbcontext info --project "$Infrastructure" --startup-project "$Api"` | Inspect the EF context and provider. |
| `dotnet ef database update --project "$Infrastructure" --startup-project "$Api"` | Apply migrations to the configured database. |

After an intentional model change, create a migration with a descriptive name, then review the generated schema changes before applying it:

```powershell
dotnet ef migrations add DescribeYourChange --project "$Infrastructure" --startup-project "$Api" --output-dir Persistence/Migrations
```

## Docker

The repository includes a multi-stage Linux Dockerfile using .NET 10 SDK and ASP.NET runtime images. Build the API image using the supplied Compose configuration:

```powershell
docker compose -f "$RepoRoot/compose.yaml" build
```

The Compose configuration currently defines **only the API image/build**. It does not configure:

- A SQL Server service or connection string override.
- Published host ports.
- The Development environment required for Swagger.
- An HTTPS certificate or TLS termination.

Configure those explicitly before using Compose as a runnable development stack. Dockerfile `EXPOSE` declarations do not publish host ports, and exposing port 8081 alone does not configure HTTPS. Inside a container, `localhost` refers to that container, not the host SQL Server instance.

## Troubleshooting

- **Swagger is missing:** use a development launch profile and navigate to `/swagger`, not `/`. Swagger is disabled outside Development.
- **Swagger has no operations:** expected at this stage; business endpoints have not been implemented.
- **SQL Server connection fails:** check the server name, credentials, network access, certificate settings, and the connection environment variable in the current shell.
- **Database tables are missing:** run the EF database update command; startup does not migrate the database.
- **Persistence tests are skipped:** configure `QUIZAPP_TEST_SQLSERVER_CONNECTION_STRING` for a dedicated test instance.
- **HTTPS certificate errors:** trust the .NET development certificate or use the HTTP launch profile for local development.

## Development guidelines

- Preserve the existing layer boundaries and keep DTOs separate from EF entities.
- Add or update validators when request contracts change, and test their registration and behavior.
- Add reviewed EF migrations for schema changes; run SQL Server tests when changing relationships or database constraints.
- Run the build and tests before submitting changes. Keep secrets and machine-specific credentials out of commits.