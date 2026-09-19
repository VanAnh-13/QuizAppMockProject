# QuizApp — Agent Memory (`MEMORY.md`)

> **Single Source of Context for AI Agents.** Consult this document on every task to understand the codebase structure,
> architectural decisions, design tokens, and technical guardrails.

---

## 1. Tech Stack & Environment

| Component                    | Technology                   | Version / Details                                                   |
|------------------------------|------------------------------|---------------------------------------------------------------------|
| **Frontend Framework**       | Angular                      | `22.1.0` (standalone components, signals, `OnPush`, zero NgModules) |
| **Frontend Language**        | TypeScript                   | `~6.0.2` (strict mode, `isolatedModules`, `target: ES2022`)         |
| **Frontend Styling**         | Tailwind CSS + CSS Variables | Tailwind `3.4.17`, custom LyQuiz Glass design tokens                |
| **Frontend Test Runner**     | Vitest + jsdom               | Vitest `4.0.8`, `@angular/build` integration                        |
| **Frontend Package Manager** | pnpm                         | `11.16.0` (run with `pnpm.cmd` on Windows)                          |
| **Backend Framework**        | ASP.NET Core                 | `.NET 10` C# solution                                               |
| **Backend ORM**              | Entity Framework Core        | `10.x` with SQL Server                                              |
| **Backend Validation**       | FluentValidation             | Assembly scanning via `AddApplication()`                            |
| **Backend Testing**          | xUnit                        | Integration tests on SQL Server                                     |
| **Containers**               | Docker / Compose             | `compose.yaml` builds `backend/Quizapp/Quizapp.Api/Dockerfile`      |

---

## 2. Repository Layout & Layer Boundaries

```
d:/Homeworks/angular/quiz_app/
├── backend/                  # .NET 10 solution
│   ├── Quizapp/
│   │   ├── Quizapp.Domain/          # Core entities, enums, FieldLimits (zero dependencies)
│   │   ├── Quizapp.Application/     # DTOs, FluentValidation, service interfaces (depends on Domain)
│   │   ├── Quizapp.Infrastructure/  # EF Core, SQL Server mappings, migrations (depends on Application, Domain)
│   │   └── Quizapp.Api/             # Controllers, JWT, middleware, composition root
│   ├── docs/                        # Architecture & contract docs (attempt-progress.md, etc.)
│   └── tests/Quizapp.Tests/         # xUnit suite & SQL Server integration tests
├── frontend/                 # Angular 22 application
│   ├── src/app/
│   │   ├── core/                    # App-wide singletons: api, auth, config, errors, utils
│   │   ├── features/                # Feature slices (Hexagonal Architecture)
│   │   │   ├── auth/                # Login, registration, session management
│   │   │   ├── quiz-explore/        # Public catalog, filters, search
│   │   │   ├── quiz-details/        # Quiz overview, rules, start button
│   │   │   └── quiz-attempt/        # Taking quiz, timer, progress save/pause/resume, submission
│   │   └── shared/                  # Reusable state types & UI components
│   │       ├── state/               # AsyncState discriminated union
│   │       └── ui/                  # async-content, toast, confirmation, dialog, brand, headers
│   ├── DESIGN.MD                    # LyQuiz Glass design system specification
│   └── AGENTS.md                    # Frontend-specific agent instructions
├── AGENTS.md                 # Repository-level agent rules & skill gates
├── MEMORY.md                 # This file (persistent agent context)
└── PROCESS.md                # Agent execution workflow & verification SOP
```

### Layer Boundary Rules

- **Backend Domain**: Entities and enums only. Never reference Application, Infrastructure, or ASP.NET Core.
- **Backend Application**: DTOs, interfaces, and FluentValidation rules. Never reference EF Core or Infrastructure.
- **Backend Infrastructure**: SQL Server persistence, EF mappings, and migrations. Never reference Api.
- **Frontend Hexagonal Architecture**:
    - `domain/`: Pure data models and types (no Angular or HTTP dependencies).
    - `application/`: Ports, abstract service interfaces, and `InjectionToken` definitions.
    - `infrastructure/`: Adapters implementing ports via `ApiClient`, provider factory functions (`provideQuiz*`).
    - `presentation/`: Standalone pages, stores, sibling `.html`/`.css` files.

---

## 3. Design System — LyQuiz Glass (`frontend/DESIGN.MD`)

### Color Palette (Blue Gradient)

- **Primary Brand**: `--brand` (`#2563eb`), `--brand-gradient` (`linear-gradient(135deg, #1d4ed8, #38bdf8)`),
  `--brand-soft` (`#eff6ff`), `--on-brand` (`#ffffff`).
- **Semantic**: `--ink` (`#0f172a`), `--muted` (`#64748b`), `--canvas` (`#f4f7fc`), `--success` (`#10b981`), `--warning`
  (`#f59e0b`), `--error` (`#ef4444`).
- **Typography**: Single typeface `Be Vietnam Pro` (loaded in `index.html`; do not add another font).

### Glass Token Hierarchy (Strict Opacity Levels)

| Token             | Opacity   | Use Case                                                           |
|-------------------|-----------|--------------------------------------------------------------------|
| `--glass-raised`  | 20% white | Sticky nav header, modal dialogs, floating toasts                  |
| `--glass-surface` | 15% white | Top-level page cards (quiz card, overview, auth card)              |
| `--glass-inset`   | 5% white  | Child elements inside surface cards (options, grid items, metrics) |
| `--glass-hover`   | 10% white | Hover/pressed state for inset or surface elements                  |

### STRICTLY PROHIBITED in Frontend Styling

- ❌ **Non-existent CSS variables**: Never use `--text-secondary`, `--primary`, `--surface`, `--border-color`,
  `--surface-hover`, `--text-primary`, `--error-text`. Use the exact tokens defined in `styles.css`.
- ❌ **Opaque surfaces**: Never use `background: #ffffff` or opaque cards on top of the fixed blue gradient background.
- ❌ **Purple / Indigo accents**: Never use `#4f46e5`, `#6366f1`, `#4338ca`, or Tailwind `indigo-*`/`violet-*`/
  `purple-*`.
- ❌ **Nested surface cards**: Never place `--glass-surface` inside another `--glass-surface`; child elements must use
  `--glass-inset`.

---

## 4. Established Frontend Patterns & Architecture

### 1. `AsyncState<T>` Pattern (`shared/state/async-state.ts`)

- Discriminated union:
  `{status: 'idle'} | {status: 'loading'} | {status: 'success', data: T} | {status: 'error', error: string}`.
- Factory helpers: `idle()`, `loading()`, `success(data)`, `error(message)`.
- UI Component: `<app-async-content [state]="state()" (retry)="reload()">` renders spinner, error state with retry
  button, or projected content.
- **Gotcha**: Angular template `@switch` does **not** narrow discriminated unions in template expressions. Always expose
  a `computed()` accessor (e.g. `errorMessage = computed(...)`) for type-safe template access.

### 2. Global Error Handling & Toast (`core/errors/`, `shared/ui/toast/`)

- `ErrorNotificationService`: Root-provided signal queue with timed auto-dismissal.
- `errorInterceptor`: Catches status `0` (network disconnection) and `5xx` (server errors), converts them to localized
  Vietnamese toast alerts, and rethrows. 4xx errors pass through for feature-level form handling.
- `ToastComponent`: Floating glass toasts at top-right, rendered in `app.html`.

### 3. Accessible Confirmation Dialog (`shared/ui/confirmation/`)

- `ConfirmationService`: Promise-based `confirm({title, message, confirmLabel, cancelLabel}) -> Promise<boolean>`.
- `ConfirmationDialogComponent`: Native `<dialog appModal>` with backdrop dismiss and keyboard accessibility.
- **Rule**: Never use blocking browser `window.confirm()`. Always use `ConfirmationService`.

### 4. HTTP Client Resilience (`core/api/retry.ts`, `core/api/api-client.ts`)

- `retryOnTransientError()`: RxJS operator retrying status `0` and `5xx` on GET/list operations with linear backoff (1s,
  2s).
- Fast-fails on 4xx client errors immediately.
- Never retry non-idempotent write operations (`POST`, `PUT`, `PATCH`, `DELETE`).

### 5. Feature Provider Functions (`provideQuiz*`)

- Clean provider encapsulation:
    - `provideQuizAttempt()`: binds `ATTEMPT_API` to `ApiQuizAttempt`.
    - `provideQuizCatalog()`: binds `QUIZ_CATALOG` to `ApiQuizCatalog`.
    - `provideQuizDetails()`: binds `QUIZ_DETAILS_CONTENT` and `ATTEMPT_API` for history.
- Used in route configurations and component `providers` arrays.

### 6. Route Resolvers (`features/quiz-details/presentation/quiz-details.resolver.ts`)

- Functional `ResolveFn` prefetching route data before navigation completes to prevent layout shift.
- Returns `null` on failure so components can gracefully show their error/retry state.

### 7. Component State Management (Signal Stores)

- Component-scoped signal stores decorated with `@Injectable()` (without `providedIn: 'root'`).
- Provided in the component's `providers: [...]` array.
- Exposes signals and computed properties (`readonly`); encapsulates state mutation methods.

---

## 5. Non-Negotiable Development Rules

1. **File Separation**: Every Angular component MUST have separate sibling `.ts`, `.html`, and `.css` files. Never use
   inline `template` or inline `styles`.
2. **Formatting**: The user's existing WebStorm formatting is the absolute source of truth. Preserve indentation, line
   endings (CRLF/LF), spacing, and wrapping. Never run Prettier on files formatted by WebStorm unless explicitly asked.
3. **Strict Typing**: Zero `any` types. Preserve TypeScript strict mode.
4. **Localization**: All UI labels, validation messages, error descriptions, and dialog copy MUST be in **Vietnamese**.
5. **Branching Model**:
    - `main`: Deploy-only. Never commit directly.
    - `dev`: Integration and testing. Base for feature work, default PR target.
    - Feature branches: `feature/web-<name>` for frontend, `feature/api-<name>` for backend.
6. **Mandatory Skills**: Always invoke an implementation skill before modifying code, and a testing skill before
   running/changing tests.
7. **SQL Server Testing**: Backend integration tests run against real SQL Server via
   `QUIZAPP_TEST_SQLSERVER_CONNECTION_STRING`. Never bypass or skip SQL tests.
