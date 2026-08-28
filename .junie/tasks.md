# Task Graph — Quiz Application (Angular + ASP.NET Core)

Source spec: `.junie/plans/quiz-app-angular-aspnet.md`
Progress is tracked live in `.junie/progress.md`.

## Task Graph Dependencies

```
T1 (backend foundation) → T2 (auth APIs) → T3 (content mgmt APIs) → T4 (quiz-taking API)
T5 (angular shell) ───────────────────────┐
T2 + T4 + T5 → T6 (angular integration) ──┴→ T7 (manager screens + packaging) ← T3
```

## Tasks

### T1 — Backend foundation: solution, SQL Server container, domain model, seeded database (spec Step 1)
- Status: ✅ DONE
- Depends on: —
- Done when: `dotnet build` passes; `docker compose up` starts SQL Server; initial migration + seed applied on API startup.
- Deliverables:
  - `backend/QuizApp.sln` (net10.0) with `QuizApp.Domain`, `QuizApp.Application`, `QuizApp.Infrastructure`, `QuizApp.Api`
  - `.config/dotnet-tools.json` pinning `dotnet-ef`
  - Domain entities: `ApplicationUser`, `ApplicationRole`, `Quiz`, `Question`, `Answer`, `QuizQuestion`, `QuizCode`, `QuizAttempt`, `UserAnswer`, `Feedback` + `QuestionType` enum
  - `AppDbContext` + one `IEntityTypeConfiguration<>` per entity (cascade rules, unique `QuizCode.Code`, `Restrict` on attempts)
  - `docker-compose.yml` (mssql service + healthcheck + volume) and `.env.example`
  - Initial EF migration + `DbSeeder` (roles Admin/Editor/User, admin account from config, sample quizzes/questions/answers)
  - Startup connection retry for slow SQL Server boot

### T2 — Authentication, authorization and account APIs (spec Step 2)
- Status: ✅ DONE
- Depends on: T1
- Done when: register/login/me/change-password/avatar endpoints work; JWT bearer + policies enforced; deactivated users blocked.
- Deliverables:
  - `IAuthService` (`RegisterAsync`, `LoginAsync`, `GetCurrentUserAsync`, `ChangePasswordAsync`) via `UserManager`/`SignInManager`
  - `JwtTokenService` (config-driven key/issuer/audience/lifetime, role claims) → `LoginResponseViewModel`
  - `AuthController`: `POST /api/auth/register`, `POST /api/auth/login`, `GET /api/auth/me`, `POST /api/auth/change-password`, `POST /api/auth/avatar`
  - JWT bearer auth, `RequireManager` (Admin|Editor) + `RequireAdmin` policies, CORS from config
  - `IsActive = false` login blocked with clear message

### T3 — Content management APIs: quizzes, question bank, users, roles, feedback (spec Step 3)
- Status: ✅ DONE
- Depends on: T2
- Done when: CRUD + assignment + publish rules + paging/search/sort on all management lists; feedback endpoint persists.
- Deliverables:
  - `IQuestionService`/`IAnswerService` (bank CRUD, answers per question, warn/delete when assigned to quizzes)
  - `IQuizService` (CRUD, add/remove question, publish rule: no activation with zero questions → 409)
  - `IUserService`/`IRoleService` (admin CRUD, roles assignment, activate/deactivate, no password editing)
  - `PagedQuery`/`PagedResult<T>` with server-side paging/search/sort + clamping; FluentValidation validators
  - `POST /api/feedback`
  - Integration tests: CRUD round-trips, assignment/removal, publish rejection, paging over 25 seeded quizzes

### T4 — Quiz-taking API: codes, attempts, server-side scoring (spec Step 4)
- Status: ✅ DONE
- Depends on: T3
- Done when: full code → prepare → take → submit → history path works; take payload has no `isCorrect`; scoring rules unit-tested.
- Deliverables:
  - `IQuizCodeService`: self-issue, validate (exists/unused/unexpired/owner), bulk generate + CSV export
  - `IQuizAttemptService`: `PrepareAsync`, `TakeAsync`, `SubmitAsync`, `GetMyAttemptsAsync`, `GetAttemptDetailAsync`; state machine `Prepared → InProgress → Submitted | Expired`
  - Server-side `startTime`, deadline = `startTime + duration`; late submit graded on what arrived, marked expired
  - `QuizForTestViewModel` projection without `isCorrect`
  - `ScoringService`: single/true-false exact, multiple-choice all-or-nothing, text trim/case-insensitive, long-answer manual review (excluded from denominator); `Score = round(correct/gradable*100)`
  - `UserAnswer` persistence, `Score`/`CorrectCount`/`TotalQuestions`; repeat submit → 409
  - Endpoints: codes self/bulk, prepare, take, submit, `GET /api/attempts/me`, `GET /api/attempts/{id}`
  - Unit tests for every scoring rule + code validation failure; integration test full journey asserting raw JSON contains no `isCorrect`

### T5 — Angular shell: layouts, routing, shared components, forms (spec Step 5)
- Status: TODO
- Depends on: — (parallel track)
- Done when: app runs at localhost:4200 with all routes, 3 layouts, shared components, mock-data forms.
- Deliverables:
  - Scaffold `frontend/quiz-app` via Angular CLI (standalone, routing, SCSS) + Bootstrap + Font Awesome
  - `layouts/`: `AuthenticationLayoutComponent`, `CustomerLayoutComponent`, `ManagerLayoutComponent` (sidebar)
  - `shared/components/`: `HeaderComponent`, `FooterComponent` (DatePipe year), `QuizCardComponent`, `SidebarComponent`, `PaginationComponent`, `ConfirmDialogComponent`
  - `DurationFormatPipe` (`15→15m`, `60→1h`, `75→1h15m`) + spec
  - `app.routes.ts` with lazy `loadChildren` for every handout route + `/403` + wildcard 404
  - Manager list/detail page shells (quiz/question/user/role)
  - Reactive forms: Login, Register, Contact, Take-a-Quiz against mock data (payload logged), inline validation
  - Home + Quizzes rendered from in-memory fixture with `@if`/`@for`

### T6 — Angular API integration, authentication, customer quiz journey (spec Step 6)
- Status: TODO
- Depends on: T5, T2, T4
- Done when: SPA works against real API with JWT; full customer journey incl. result + history; profile area; required specs pass.
- Deliverables:
  - `core/models` ViewModels + `core/services` interfaces, `InjectionToken`s, HttpClient impls (`IQuizService`, `IQuestionService`, `IUserService`, `IRoleService`, `IAuthService`)
  - `getAll()` unwraps `PagedResult.items`; `getPaged(query)` alongside
  - `authInterceptor` (bearer + 401 → login), `errorInterceptor` (ProblemDetails → inline/dialog)
  - `authGuard`, `roleGuard` (returnUrl to `/auth/login`, non-managers to `/403`)
  - Auth state signals (`isAuthenticated`, `isManager`, `getCurrentUser`), token persistence, profile dropdown + logout
  - Live data on Home/Quizzes; Contact → `POST /api/feedback`
  - Customer journey: Start/code → prepare → take (countdown + per-type controls) → unanswered confirmation → submit → result → attempts history + detail
  - Profile: view/update, change password, avatar upload
  - Specs: `QuizService` (all methods via `HttpTestingController`), `AuthService` (login/register), `HomeComponent`, both guards

### T7 — Management screens and full-stack packaging (spec Step 7)
- Status: TODO
- Depends on: T6, T3
- Done when: admins manage all content from UI; `docker compose up` runs mssql + api + web; full verification pass green.
- Deliverables:
  - Manager lists (Quiz/Question/User/Role) with server-side paging/search/sort using `PaginationComponent` + `getPaged`
  - Quiz add/edit (+ *Show Questions* panel edit-mode only; assign/remove questions)
  - Question add/edit (+ *Show Answers* panel edit-mode only; all six types; mark correct answers)
  - User add/edit (no password editing; activate/deactivate; role assignment); Role add/edit
  - Bulk quiz-code screen: pick quiz, select users, generate, display, CSV download
  - Delete flows via `ConfirmDialogComponent` incl. question-in-use warning
  - `environments/environment.ts` / `environment.production.ts` for API base URL
  - Dockerfiles: API (multi-stage), frontend (ng build → nginx SPA fallback + API proxy); `docker-compose.yml` extended to mssql + api + web with healthchecks/ordered start
  - `README.md`: prerequisites, `.env`, compose up, dev commands, seeded admin credentials, test commands
  - Final verification: `dotnet build`, `dotnet test`, `npx ng build`, `npx ng test --watch=false --browsers=ChromeHeadless`, curl smoke test over composed stack
