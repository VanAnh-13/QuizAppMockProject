# QuizApp — Agent Operating Process (`PROCESS.md`)

> **Standard Operating Procedure (SOP) for AI Agents.** Every coding task in this repository must follow this sequence
> from pre-flight to completion. Do not skip or reorder stages.

---

## Process Overview Flowchart

```
[Pre-Flight Inspection]
       │
       ▼
[Skill Invocation Gate] ──(Missing skill?)──► [HALT & Ask User]
       │ (Skill loaded)
       ▼
[Plan & Design Check]
       │
       ▼
[Incremental Implementation]
       │
       ▼
[Testing & Verification] ──(Failure?)──► [Fix & Re-verify]
       │ (100% Pass)
       ▼
[Formatting & Style Gate]
       │
       ▼
[Atomic Commit & Evidence Report]
```

---

## Stage 1: Pre-Flight & Workspace Inspection

1. **Check Git Status & Branch**:
   ```powershell
   git status
   git branch --show-current
   ```
2. **Preserve Existing Work**:
    - Inspect any modified files or untracked files in the working tree.
    - **Never discard, overwrite, or delete** untracked files unless explicitly requested by the user.
3. **Branching Model Verification**:
    - Ensure you are NOT working on `main` (deploy-only) or `dev` (integration).
    - Feature branches must branch off `dev` using standard naming:
        - Frontend features: `feature/web-<feature-name>`
        - Backend features: `feature/api-<feature-name>`

---

## Stage 2: Mandatory Skill Invocation Gate

> ⚠️ **MANDATORY GATE**: You must invoke the appropriate skill before editing code and before testing. Merely mentioning
> a skill name does NOT satisfy this rule.

1. **Before Writing Code**:
    - Inspect available skills.
    - Invoke the matching implementation skill using `view_file` on its `SKILL.md`:
        - Standard feature work: `incremental-implementation`
        - Refactoring / cleanup: `code-simplification`
        - API contracts / interfaces: `api-and-interface-design`
        - Bug fixing: `diagnosing-bugs`
2. **Before Writing Tests or Running Test Commands**:
    - Invoke a testing skill:
        - Unit / integration testing: `tdd`
        - Browser / UI testing: `webapp-testing`
3. **If No Skill Is Available**:
    - **STOP IMMEDIATELY**. Do not write code or execute tests.
    - Explain the limitation clearly to the user and request permission to proceed.

---

## Stage 3: Context Loading & Design Alignment

1. **Read Target Files & Tests First**:
    - Read the implementation file (s) and their corresponding `.spec.ts` or test classes before modifying them.
2. **Consult `MEMORY.md`**:
    - Verify tech stack versions, architectural boundaries, and known pitfalls.
3. **Frontend Visual / CSS Changes**:
    - **Mandatory**: Read `frontend/DESIGN.MD`.
    - Verify tokens against `:root` in `frontend/src/styles.css`.
    - Check glass hierarchy (`--glass-raised`, `--glass-surface`, `--glass-inset`, `--glass-hover`).
    - Check prohibited tokens (no `--text-secondary`, no purple/indigo, no opaque white backgrounds).
4. **Backend Entity / DTO Changes**:
    - Check limits in `Quizapp.Domain/Constants/FieldLimits.cs`.
    - Check DTO contracts in `tests/Quizapp.Tests/Application/DtoContractTests.cs`.
    - Check EF mappings in `Quizapp.Infrastructure/Persistence/Configurations`.

---

## Stage 4: Implementation Guidelines

### Frontend Work (`frontend/`)

- **Three-file rule**: Every component must have separate `.ts`, `.html`, and `.css` files. Never use inline `template:`
  or inline `styles:`.
- **Typing**: TypeScript strict mode enabled. Zero `any`.
- **HTTP via `ApiClient`**: Never inject raw `HttpClient` into components. Use `ApiClient` inside infrastructure
  adapters.
- **Resilience**: Only idempotent GET/list queries use `retryOnTransientError()`. Do not retry mutations (`POST`, `PUT`,
  `PATCH`, `DELETE`).
- **Confirmation dialogs**: Use `ConfirmationService.confirm()`. Never call browser `window.confirm()`.
- **UI Copy**: All user-facing strings must be in **Vietnamese**.

### Backend Work (`backend/`)

- **C# Conventions**: File-scoped namespaces, 4-space indentation, PascalCase members, `_camelCase` private fields.
- **Layer Boundary Enforcement**:
    - `Domain`: zero dependencies.
    - `Application`: references `Domain` only.
    - `Infrastructure`: references `Application` and `Domain`. Register all EF configurations explicitly in
      `QuizAppDbContext.cs`.
    - `Api`: references `Application` and `Infrastructure`.
- **Validation**: Place FluentValidation validators alongside DTOs in `Application`. Verified via `AddApplication()`
  scanning.

---

## Stage 5: Verification & Testing Workflow

### Frontend Verification Sequence

Execute in `d:\Homeworks\angular\quiz_app\frontend`:

```powershell
# 1. Run all unit tests (must be 100% pass)
pnpm.cmd test --watch=false

# 2. Verify production build and template type checking
pnpm.cmd build

# 3. Check for whitespace, newline, and formatting anomalies
git diff --check
```

### Backend Verification Sequence

Execute from repository root:

```powershell
$RepoRoot = 'D:/Homeworks/c#/Quizapp'
$Solution = "$RepoRoot/Quizapp.sln"
$Tests = "$RepoRoot/tests/Quizapp.Tests/Quizapp.Tests.csproj"

# Ensure SQL Server test connection string is populated from configuration
$env:QUIZAPP_TEST_SQLSERVER_CONNECTION_STRING = "..."

dotnet restore "$Solution"
dotnet build "$Solution" --no-restore
dotnet test "$Tests" --no-build --no-restore
```

- Confirm SQL Server integration tests are NOT skipped. If SQL Server is unavailable, explicitly report the blocker.

---

## Stage 6: Code Formatting & Quality Gate

- **WebStorm is Source of Truth**: Match existing formatting around changed lines. Do not reformat untouched code.
- **Check Diff for Whitespace**:
  ```powershell
  git diff --check dev..HEAD
  ```
  Fix any blank lines at EOF or trailing whitespace before committing.

---

## Stage 7: Git Commit & PR Workflow

1. **Commit Message Format**: Follow Conventional Commits:
    - `feat(scope): brief description`
    - `fix(scope): brief description`
    - `test(scope): brief description`
    - `refactor(scope): brief description`
    - `style(scope): formatting without logic change`
2. **Atomic Commits**: Commit by logical slice or feature rather than a single massive commit.
3. **Clean Diffs**: Never stage secrets, machine credentials, or build artifacts (`dist/`, `bin/`, `obj/`,
   `.artifacts/`).
4. **Pull Requests**: Target `dev` branch, never `main`.

---

## Stage 8: Evidence Reporting & Final Handoff

In your final response to the user, always provide:

1. **Skills Invoked**: Explicitly list which skills were read and applied during implementation and testing.
2. **Commands Executed**: Exact shell commands that were run.
3. **Observed Results**: Actual test counts (e.g. `24/24 test files passed, 157/157 tests passed`), build status, and
   diff clean checks.
4. **Any Skipped or Unverified Areas**: Note any unverified live routes or environments with explicit justification.
