# Quizapp

A quiz-management platform with a .NET backend and an Angular frontend.

## Repository Layout

```
Quizapp/
├── backend/              # .NET 10 solution — API, domain, application, infrastructure
├── frontend/             # Angular + TypeScript web client
├── compose.yaml          # Docker Compose — SQL Server, migrations, API, frontend
├── .editorconfig         # Shared editor settings
├── .gitignore            # Shared ignore rules
├── AGENTS.md             # Agent instructions and development guidelines
└── README.md             # This file
```

| Directory                | README                                | Status                                                                                                  |
| ------------------------ | ------------------------------------- | ------------------------------------------------------------------------------------------------------- |
| [`backend/`](backend/)   | [Backend README](backend/README.md)   | Services, controllers, JWT authentication, EF migrations, and tests                                     |
| [`frontend/`](frontend/) | [Frontend README](frontend/README.md) | Quiz discovery, authentication, attempts, submission, and history |

## Tech Stack

| Layer            | Technology                                                              |
| ---------------- | ----------------------------------------------------------------------- |
| Backend          | ASP.NET Core 10, Entity Framework Core 10, SQL Server, FluentValidation |
| Frontend         | Angular 22, TypeScript, Tailwind CSS, Angular Signals                   |
| Testing          | xUnit (backend), Vitest/jsdom (frontend)                                |
| Containerization | Docker, Docker Compose                                                  |

## Quick Start

### Full Stack with Docker

Install Docker with Compose and start Docker before running these commands from the repository root:

```powershell
Copy-Item '.env.example' '.env'
```

Edit `.env`: replace `SA_PASSWORD` with a valid SQL Server administrator password and `JWT_SIGNING_KEY` with a random signing key of at least 32 bytes. The template includes a PowerShell command to generate a key. Keep an existing `.env` when updating your checkout.

```powershell
docker compose up -d --build
```

Compose starts SQL Server, runs database migrations, then starts the API and Angular frontend. The API waits for migrations to succeed, including on a fresh database volume and when sample data is disabled.

Open `http://localhost:4200` for the frontend or `http://localhost:5269/swagger` for the API documentation. The default environment is Development, with sample data enabled. `.env` also controls the published ports and database name; SQL Server defaults to port 1433. See [Docker setup and troubleshooting](backend/README.md#docker) for startup order, logs, and persistent storage.

### Backend

Configure SQL Server and JWT using the [backend setup instructions](backend/README.md#2-configure-sql-server-and-jwt) before starting the API.

```powershell
dotnet restore "D:/Homeworks/c#/Quizapp/backend/Quizapp.sln"
dotnet build "D:/Homeworks/c#/Quizapp/backend/Quizapp.sln" --no-restore
dotnet run --project "D:/Homeworks/c#/Quizapp/backend/Quizapp/Quizapp.Api/Quizapp.Api.csproj" --launch-profile https
```

Full setup instructions (SQL Server, migrations, Docker, testing) → [Backend README](backend/README.md)

### Frontend

Run these commands from the repository root:

```powershell
Set-Location "./frontend"
pnpm install
$env:QUIZAPP_API_TARGET = 'http://localhost:5269'
pnpm start
```

Open `http://localhost:4200/`; run the backend HTTP profile first so the public quiz catalog is available. Full frontend instructions → [Frontend README](frontend/README.md)

## Branching Model

| Branch      | Purpose                                                                                |
| ----------- | -------------------------------------------------------------------------------------- |
| `main`      | Deploy-only. Advances through reviewed pull requests from `dev`. Carries release tags. |
| `dev`       | Integration and testing. Base for feature work and default PR target.                  |
| `feature/*` | Short-lived feature branches merged into `dev` via pull request.                       |

**Naming convention:**

- Backend features: `feature/api-<name>` (e.g., `feature/api-auth-login`)
- Frontend features: `feature/web-<name>` (e.g., `feature/web-quiz-list`)

Build and test successfully before opening a pull request. Never commit secrets or machine-specific credentials.

## Development Guidelines

- Keep backend changes inside `backend/`. Frontend work belongs in `frontend/`, not inside the .NET solution tree.
- Preserve layer boundaries — see the [Backend README](backend/README.md) for architecture details.
- Add or update validators when request contracts change.
- Add reviewed EF migrations for schema changes.
- Run the build and tests before submitting changes.
- See [AGENTS.md](AGENTS.md) for detailed editing conventions and guardrails.

## License

This project is for educational purposes.
