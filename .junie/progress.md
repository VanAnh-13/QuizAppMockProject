# Progress Log — Quiz Application

Spec: `.junie/plans/quiz-app-angular-aspnet.md` · Tasks: `.junie/tasks.md`
Last updated: 2026-08-27

## Status Board

| Task | Description | Status |
| --- | --- | --- |
| T1 | Backend foundation (solution, mssql compose, domain, migrations, seed) | ✅ DONE |
| T2 | Authentication, authorization & account APIs | ✅ DONE |
| T3 | Content management APIs (quiz/question/user/role/feedback) | ✅ DONE |
| T4 | Quiz-taking API (codes, attempts, scoring) | 🔄 IN PROGRESS |
| T5 | Angular shell (layouts, routing, shared components, forms) | ⏳ TODO |
| T6 | Angular API integration & customer quiz journey | ⏳ TODO |
| T7 | Management screens & full-stack packaging | ⏳ TODO |

## Log

### 2026-08-27
- Repository initialized (git), local identity: Van Anh <levananh13062004@gmail.com>.
- Environment verified: .NET SDK 10.0.303 (also 8.0.424), Node v24.18.0, npm 11.16.0, Docker 29.7.2 + Compose v5.4.0 (daemon running).
- `.gitignore` added; spec `.docx` files committed unchanged.
- Task graph created from spec Delivery Steps (T1–T7).
- **T1 DONE**: `backend/QuizApp.sln` (net10.0) with Domain/Application/Infrastructure/Api; tool manifest pinned at `backend/.config/dotnet-tools.json` (dotnet-ef 10.0.11); EF Core 10.0.11 packages; full domain model (10 entities + 2 enums); `AppDbContext` with per-entity configurations; `docker-compose.yml` (mssql 2022 + healthcheck, healthy) + `.env.example`; `InitialCreate` migration; `DbSeeder` (3 roles, admin + demo user from config, 4 quizzes/8 questions/19 answers); startup connection retry.
  - Fixes along the way: SQL Server error 1785 (multiple cascade paths) → `UserAnswer` snapshot FKs use `ClientSetNull`; tool manifest moved to `.config/`; `QuizApp.Api` converted to Web SDK; parallel racing builds caused a missing-migration DLL → clean rebuild fixed it.
  - Verified: `dotnet build` green; `GET /health` → `{"status":"healthy"}`; DB row counts confirmed via sqlcmd.
- **T2 DONE**: `IAuthService` (register/login/me/change-password/update-profile/set-avatar) in Application with FluentValidation validators; `JwtTokenService` (config-driven key/issuer/audience/lifetime, role claims); `LocalFileStorage` (avatars, content-type whitelist); `AuthController` (register/login/me/change-password/avatar/profile); JWT bearer auth + `RequireManager`/`RequireAdmin` policies; CORS from config; global `ProblemDetailsExceptionHandler` (RFC 7807 + traceId + field errors); Swagger with Bearer; avatar static files at `/avatars`.
  - Fixes: Swashbuckle 10 / Microsoft.OpenApi v2 API changes (flattened namespace, `OpenApiSecuritySchemeReference`, `AddSecurityRequirement(Func<OpenApiDocument,…>)`).
  - Verified via curl: register 201 (+role `User`), login returns `{userInformation, token, expires}`, `/me` with bearer works, wrong password → 401 ProblemDetails, no token → 401, duplicate username/email → 409 with `errors.{userName,email}`.
- **T3 DONE**: `IQuizService`/`IQuestionService`/`IAnswerService`/`IUserService`/`IRoleService`/`IFeedbackService` in Application over an `IAppDbContext` abstraction (EF Core base package only); `PagedQuery`/`PagedResult` + `PagingExtensions` (SQL-side search/sort/page, clamped to Paging:DefaultPageSize/MaxPageSize); FluentValidation validators per command DTO; controllers for quizzes (public list/active/detail + manager CRUD + question assign/remove with `quizQuestionId`), questions (CRUD + `delete-preview` warning + answers), answers, users (admin CRUD + status + roles), roles (CRUD), feedback (anonymous POST).
  - Business rules implemented: publish-with-zero-questions → 409 (quiz stays draft); question delete → warns when assigned, removes from quizzes, nulls UserAnswer refs (history snapshots survive); quiz delete with attempts → soft-deactivate; user edit never touches password; built-in `Admin` role undeletable.
  - New `QuizApp.Api.IntegrationTests` (xunit + WebApplicationFactory, isolated per-run DB created+migrated+seeded then dropped): 12 tests — 401/403/201 authz matrix, quiz CRUD round-trip, publish rejection, assign/remove, paging/search/sort/clamping over 25 quizzes, delete-warning, deactivated-user 403, duplicate-register 409.
  - Root-caused two subtle bugs: (1) `Guid.ToString()` inside EF projections → SQL `CONVERT` returns UPPERCASE (materialize before mapping); (2) WebApplicationFactory's deferred host applies `ConfigureAppConfiguration` after the entry point configures services → connection string + JWT params are now read post-Build via DI/options pattern; entry-point auto-migrate disabled under the factory, which migrates+seeds itself in `InitializeAsync`.
  - Verified: **12/12 integration tests green**.
- Started T4.
