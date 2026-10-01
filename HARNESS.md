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

## Frontend agent runtime (WSL)

Run frontend Node, pnpm, Angular, tests, and builds **inside WSL**, not in the Windows host shell.
The frontend checkout is available at `/mnt/e/QuizAppMockProject/frontend` in WSL. From a Windows
terminal already in `frontend/`, `wsl.exe --exec bash -lic 'pnpm test --watch=false'` inherits that
working directory; use the same pattern for `pnpm build` and `pnpm run check:rules`. In Git Bash,
set `MSYS_NO_PATHCONV=1` before `wsl.exe` when passing absolute Linux paths so Git Bash does not
rewrite them. Do not use `pnpm.cmd` from the host shell for frontend verification.

Check `command -v pnpm` and `pnpm --version` in WSL before testing. If the first `pnpm` on `PATH`
reports `the global target of the pnpm shim points back at the shim`, run the pnpm executable from
the active WSL Node/NVM installation instead of the self-referential shim. Do not reinstall
packages or change the lockfile solely to work around that shim.

On a memory-limited WSL instance, the unbounded full Vitest run may exit before executing any tests.
Retry the full suite with a temporary runner config outside the checkout (no project config changes):

```bash
printf '%s\n' 'export default { test: { maxWorkers: 2, fileParallelism: false } };' > /tmp/quizapp-vitest.config.mjs
pnpm test --watch=false --runner-config=/tmp/quizapp-vitest.config.mjs
```

Then run `pnpm run check:rules`, `pnpm build`, and `git diff --check` separately instead of
`pnpm run verify` if that script's default test run exhausts memory.

---

## 2. Automated Rule & Architecture Checker

Script location: [`scripts/check-rules.mjs`](file:///d:/Homeworks/angular/quiz_app/scripts/check-rules.mjs)  
NPM command: `pnpm run check:rules` (inside `frontend/`)

### Rules Enforced by the Harness

| Rule ID                | What It Checks                                                                                                                                                       | Why It Matters                                                                     |
|------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------|
| `DESIGN_TOKEN`         | Flags any usage of non-existent CSS variables (`--text-secondary`, `--primary`, `--surface`, `--border-color`, `--surface-hover`, `--text-primary`, `--error-text`). | Prevents broken styles; forces use of tokens defined in `frontend/src/styles.css`. |
| `BRAND_COLOR`          | Flags prohibited colors (`#4f46e5`, `#6756cd`, `#4338ca`, `#6366f1`, `#7060da`, `#eeebff`, and Tailwind `indigo-*`, `violet-*`, `purple-*` classes).                 | Enforces the Liquid Glass Blue Gradient brand palette.                             |
| `COMPONENT_SEPARATION` | Flags `@Component` decorators containing inline `template:` or inline `styles:`/`style:`.                                                                            | Enforces three-file component architecture (`.ts`, `.html`, `.css`).               |
| `NO_ANY`               | Flags any `: any`, `as any`, or `<any>` in non-spec source files.                                                                                                    | Enforces strict TypeScript compile-time safety.                                    |
| `LAYER_BOUNDARY`       | Flags invalid cross-layer imports (e.g., Core depending on Feature presentation/infrastructure, Domain depending on `@angular/*`).                                   | Preserves Hexagonal Architecture and prevents circular dependencies.               |
| `RAW_HTTP_CLIENT`      | Flags Presentation components directly injecting `HttpClient`.                                                                                                       | Enforces HTTP isolation via `ApiClient` and infrastructure adapters.               |

### Running the Rule Checker

From `frontend/` inside WSL:

```bash
pnpm run check:rules
```

Alternatively, run `node scripts/check-rules.mjs` from the repository root inside WSL.

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

The PowerShell runners below are for Windows setups with `pnpm.cmd` available. For this WSL
frontend environment, run `pnpm run verify` from `frontend/` inside WSL, then run
`git diff --check`; the npm script does not include the whitespace check.

### 1. Frontend Verification ([
`scripts/verify-frontend.ps1`](file:///d:/Homeworks/angular/quiz_app/scripts/verify-frontend.ps1))

Runs rule checking, Vitest test suite, production build, and git whitespace checks:

```powershell
.\scripts\verify-frontend.ps1
```

Or from `frontend/` inside WSL:

```bash
pnpm run verify
git diff --check
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
- [ ] `pnpm test --watch=false` in WSL passes 100% of tests.
- [ ] `pnpm build` in WSL completes without errors or bundle warnings.
- [ ] `git diff --check` reports zero whitespace or EOF newline anomalies.
- [ ] All new components have corresponding `.spec.ts` files using `src/testing/` fixtures/mocks where applicable.
