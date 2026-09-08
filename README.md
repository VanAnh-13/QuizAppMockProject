# Quizapp

A quiz-management platform with a .NET backend and an Angular frontend (planned).

## Repository Layout

```
Quizapp/
├── backend/              # .NET 10 solution — API, domain, application, infrastructure
├── frontend/             # Angular + TypeScript web client (not yet implemented)
├── compose.yaml          # Docker Compose — API container build
├── .editorconfig         # Shared editor settings
├── .gitignore            # Shared ignore rules
├── AGENTS.md             # Agent instructions and development guidelines
└── README.md             # This file
```

| Directory | README | Status |
| --- | --- | --- |
| [`backend/`](backend/) | [Backend README](backend/README.md) | Services, controllers, JWT authentication, EF migrations, and tests |
| [`frontend/`](frontend/) | [Frontend README](frontend/README.md) | 🚧 Not yet implemented |

## Tech Stack

| Layer | Technology |
| --- | --- |
| Backend | ASP.NET Core 10, Entity Framework Core 10, SQL Server, FluentValidation |
| Frontend | Angular, TypeScript (planned) |
| Testing | xUnit (backend), Jasmine/Jest (frontend — planned) |
| Containerization | Docker, Docker Compose |

## Quick Start

### Backend

Configure SQL Server and JWT using the [backend setup instructions](backend/README.md#2-configure-sql-server-and-jwt) before starting the API.

```powershell
dotnet restore "D:/Homeworks/c#/Quizapp/backend/Quizapp.sln"
dotnet build "D:/Homeworks/c#/Quizapp/backend/Quizapp.sln" --no-restore
dotnet run --project "D:/Homeworks/c#/Quizapp/backend/Quizapp/Quizapp.Api/Quizapp.Api.csproj" --launch-profile https
```

Full setup instructions (SQL Server, migrations, Docker, testing) → [Backend README](backend/README.md)

### Frontend

Not yet implemented → [Frontend README](frontend/README.md)

## Branching Model

| Branch | Purpose |
| --- | --- |
| `main` | Deploy-only. Advances through reviewed pull requests from `dev`. Carries release tags. |
| `dev` | Integration and testing. Base for feature work and default PR target. |
| `feature/*` | Short-lived feature branches merged into `dev` via pull request. |

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
