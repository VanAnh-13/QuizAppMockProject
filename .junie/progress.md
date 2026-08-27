# Progress Log — Quiz Application

Spec: `.junie/plans/quiz-app-angular-aspnet.md` · Tasks: `.junie/tasks.md`
Last updated: 2026-08-27

## Status Board

| Task | Description | Status |
| --- | --- | --- |
| T1 | Backend foundation (solution, mssql compose, domain, migrations, seed) | ✅ DONE |
| T2 | Authentication, authorization & account APIs | ✅ DONE |
| T3 | Content management APIs (quiz/question/user/role/feedback) | 🔄 IN PROGRESS |
| T4 | Quiz-taking API (codes, attempts, scoring) | ⏳ TODO |
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
- Started T3.
