# QuizApp — Agent Verification & Testing Harness (`HARNESS.md`)

> **Comprehensive tooling, automated guardrails, and testing harness for AI agents and developers working on QuizApp.**

---

## 1. Harness Overview

The QuizApp Agent Harness is a multi-layered verification system designed to ensure every agent modification respects:

1. **Design System Tokens (`frontend/DESIGN.MD`)**: Prevents phantom CSS variables, purple/indigo leaks, and opaque
   cards.
2. **Component Architecture**: Enforces separation of `.ts`, `.html`, and `.css` files.
3. **Type Safety**: Enforces strict typing with zero `any`.
4. **Hexagonal Boundaries**: Validates that Core, Domain, and Presentation layers follow dependency rules.
5. **Standardized Testing**: Provides ready-to-use mocks and fixtures (`frontend/src/testing/`) so agents write robust,
   non-flaky tests without boilerplate.
6. **Automated Verification Pipelines**: Fast one-command verification scripts.

---

## 2. Automated Rule & Architecture Checker

Script location: [`scripts/check-rules.mjs`](file:///d:/Homeworks/angular/quiz_app/scripts/check-rules.mjs)  
NPM command: `pnpm run check:rules` (inside `frontend/`)

### Rules Enforced by the Harness

| Rule ID                | What It Checks                                                                                                                                                       | Why It Matters                                                                     |
|------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------|
| `DESIGN_TOKEN`         | Flags any usage of non-existent CSS variables (`--text-secondary`, `--primary`, `--surface`, `--border-color`, `--surface-hover`, `--text-primary`, `--error-text`). | Prevents broken styles; forces use of tokens defined in `frontend/src/styles.css`. |
| `BRAND_COLOR`          | Flags prohibited colors (`#4f46e5`, `#6756cd`, `#4338ca`, `#6366f1`, `#7060da`, `#eeebff`, and Tailwind `indigo-*`, `violet-*`, `purple-*` classes).                 | Enforces the LyQuiz Glass Blue Gradient brand palette.                             |
| `COMPONENT_SEPARATION` | Flags `@Component` decorators containing inline `template:` or inline `styles:`/`style:`.                                                                            | Enforces three-file component architecture (`.ts`, `.html`, `.css`).               |
| `NO_ANY`               | Flags any `: any`, `as any`, or `<any>` in non-spec source files.                                                                                                    | Enforces strict TypeScript compile-time safety.                                    |
| `LAYER_BOUNDARY`       | Flags invalid cross-layer imports (e.g., Core depending on Feature presentation/infrastructure, Domain depending on `@angular/*`).                                   | Preserves Hexagonal Architecture and prevents circular dependencies.               |
| `RAW_HTTP_CLIENT`      | Flags Presentation components directly injecting `HttpClient`.                                                                                                       | Enforces HTTP isolation via `ApiClient` and infrastructure adapters.               |

### Running the Rule Checker

```powershell
# From repository root:
node scripts/check-rules.mjs

# Or from frontend directory:
cd frontend
pnpm run check:rules
```

---

## 3. Frontend Testing Harness (`frontend/src/testing/`)

The testing harness is located in `frontend/src/testing/` and is automatically excluded from production builds
(`tsconfig.app.json`) while included in test runs (`tsconfig.spec.json`).

### Available Test Doubles

#### 1. `MockApiClient` ([
`mock-api-client.ts`](file:///d:/Homeworks/angular/quiz_app/frontend/src/testing/mock-api-client.ts))

Provides ViTest spies for HTTP calls:

```typescript
import {MockApiClient} from '../../testing';

const mockApi = new MockApiClient();
mockApi.get.mockResolvedValue({id: '123'});
mockApi.post.mockResolvedValue({success: true});

// Reset all spies between tests:
mockApi.reset();
```

#### 2. `MockAuthSession` ([
`mock-auth-session.ts`](file:///d:/Homeworks/angular/quiz_app/frontend/src/testing/mock-auth-session.ts))

Controls reactive authentication state without touching browser storage:

```typescript
import {MockAuthSession} from '../../testing';

const auth = new MockAuthSession();

// Set user as signed in:
auth.setAuthenticated({fullName: 'Nguyễn Văn A'});
expect(auth.isAuthenticated()).toBe(true);

// Set anonymous:
auth.setAnonymous();
expect(auth.isAuthenticated()).toBe(false);
```

#### 3. `MockConfirmationService` ([
`mock-confirmation.ts`](file:///d:/Homeworks/angular/quiz_app/frontend/src/testing/mock-confirmation.ts))

Simulates user confirmation dialog choices without DOM interaction:

```typescript
import {MockConfirmationService} from '../../testing';

const confirmation = new MockConfirmationService();
confirmation.setAutoResponse(true); // Automatically accept dialogs

const result = await confirmation.confirm({title: 'Xóa bài thi?', message: '...'});
expect(result).toBe(true);
expect(confirmation.lastOptions?.title).toBe('Xóa bài thi?');
```

#### 4. `MockErrorNotificationService` ([
`mock-error-notification.ts`](file:///d:/Homeworks/angular/quiz_app/frontend/src/testing/mock-error-notification.ts))

Inspects toast messages dispatched by components or interceptors:

```typescript
import {MockErrorNotificationService} from '../../testing';

const errors = new MockErrorNotificationService();
errors.show('Không thể tải dữ liệu.');

expect(errors.hasMessage('Không thể tải')).toBe(true);
expect(errors.notifications().length).toBe(1);
errors.clear();
```

### Pre-Canned Fixture Factories ([
`fixtures.ts`](file:///d:/Homeworks/angular/quiz_app/frontend/src/testing/fixtures.ts))

Create fully typed, realistic domain models with partial override support:

```typescript
import {
    createTestQuizSummary,
    createTestQuizDetailsSnapshot,
    createTestAttemptStart,
    createTestAttemptProgress,
    createTestAttemptResult,
    createTestUser
} from '../../testing';

// Default quiz summary:
const summary = createTestQuizSummary();

// Custom overrides:
const customQuiz = createTestQuizSummary({title: 'Thi thử Angular 22', durationMinutes: 60});
const testUser = createTestUser({username: 'learner01'});
const progress = createTestAttemptProgress({remainingSeconds: 1200});
```

---

## 4. Verification Automation Scripts

Automated PowerShell runners that agents should execute during Stage 5 of `PROCESS.md`:

### 1. Frontend Verification ([
`scripts/verify-frontend.ps1`](file:///d:/Homeworks/angular/quiz_app/scripts/verify-frontend.ps1))

Runs rule checking, Vitest test suite, production build, and git whitespace checks:

```powershell
.\scripts\verify-frontend.ps1
```

Or via NPM:

```powershell
cd frontend
pnpm run verify
```

### 2. Backend Verification ([
`scripts/verify-backend.ps1`](file:///d:/Homeworks/angular/quiz_app/scripts/verify-backend.ps1))

Restores packages, builds the solution, and runs xUnit tests against SQL Server:

```powershell
.\scripts\verify-backend.ps1
```

### 3. Full Monorepo Gate ([`scripts/verify-all.ps1`](file:///./quiz_app/scripts/verify-all.ps1))

Verifies both frontend and backend subtrees sequentially:

```powershell
.\scripts\verify-all.ps1
```

---

## 5. Agent Verification Checklist

Before reporting completion or committing changes, an agent must verify:

- [ ] `pnpm run check:rules` exits with `0` (Zero token, style, or boundary violations).
- [ ] `pnpm.cmd test --watch=false` passes 100% of tests.
- [ ] `pnpm.cmd build` completes without errors or bundle warnings.
- [ ] `git diff --check` reports zero whitespace or EOF newline anomalies.
- [ ] All new components have corresponding `.spec.ts` files using `src/testing/` fixtures/mocks where applicable.
