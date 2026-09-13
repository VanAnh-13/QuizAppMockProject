# Agent instructions

## Scope and starting point

These instructions apply to the entire Quizapp repository. Follow any more specific instructions in the subtree you edit.

1. Inspect the working tree before editing. Preserve existing user changes and untracked files; an untracked file is not necessarily disposable.
2. Read the affected implementation and its tests before proposing a change. State a short plan, then keep edits scoped to the request.
3. For environment setup, migration commands, local URLs, or Docker work, read `D:/Homeworks/c#/Quizapp/README.md`. Recheck the relevant project/configuration files before changing dependencies or documenting capabilities.

This is a monorepo. The .NET solution lives in `D:/Homeworks/c#/Quizapp/backend`; shared documentation and repository configuration stay at the root. A future web client belongs in its own top-level directory, not inside the .NET solution tree.

The application is a .NET 10 backend with controllers and services for authentication, user/role management, quiz management, quiz taking, and history. JWT settings are validated before startup, and authentication checks the current user's security stamp. Starting an attempt uses POST and persists its owner, deadline and quiz snapshot. Progress can be saved, paused (freezing time), and resumed on the same attempt. The frontend triggers submission; expired attempts grade only saved answers. Progress writes use revisions to reject stale updates. See backend/docs/attempt-progress.md for the contract. Development OpenAPI/Swagger is available. The Angular frontend uses the public quiz catalog for exploration and details, then authenticated API calls for registration, login, attempt progress, submission, and history.

## Branching model

- `main` is deploy-only. Do not commit or push to it directly; it advances through reviewed pull requests from `dev` and carries release tags.
- `dev` is the integration and testing branch. It is the base for feature work and the default pull-request target.
- Feature work happens on short-lived branches merged into `dev` through pull requests. Scope the name so backend and frontend work stay distinguishable, for example `feature/api-auth-login` or `feature/web-quiz-list`.
- Build and test successfully before opening a pull request. Never commit secrets or machine-specific credentials.

## Mandatory skill usage

Skill invocation is a required gate, not an optional recommendation.

1. **Before coding:** inspect the skills available in the current environment and invoke the skill that matches the task before creating, modifying, or refactoring source code. Examples, when available: `implement`, `diagnosing-bugs`, or `code-simplification`.
2. **Before testing:** invoke an appropriate testing skill before creating or modifying tests, or executing test commands. Examples, when available: `tdd`, `qa`, or `webapp-testing`. An earlier coding-skill invocation does not waive this testing requirement. If one skill explicitly covers both implementation and testing, invoke it at the start and follow its testing workflow.
3. **Execute the workflow:** use the actual skill-invocation mechanism and follow the returned instructions. Merely naming a skill, proposing to use it, or running build/test commands directly does not satisfy this requirement.
4. **Handle unavailable skills:** if no suitable skill is available, invocation fails, or no invocation mechanism exists, stop before the affected coding/testing action. Explain the limitation and ask the user how to proceed; do not silently bypass the gate.
5. **Report evidence:** in the completion summary, identify the skills invoked, commands actually executed, and observed test results. Never claim an invocation or validation that did not occur.

These gates also apply to small fixes, regression tests, and validation runs after documentation changes. Select skills from the current environment rather than assuming the example names exist.

## Layer boundaries

- **Domain:** entities, enums, and shared limits; keep it independent of Application, Infrastructure, ASP.NET Core, and EF Core.
- **Application:** DTOs, FluentValidation validators, and their registration; depends on Domain, not Infrastructure or ASP.NET Core/EF Core.
- **Infrastructure:** SQL Server persistence, EF mappings, migrations, and persistence registration; references Application and Domain, not Api.
- **Api:** composition root and HTTP-facing behavior; references Application and Infrastructure.
- **Tests:** extend the existing xUnit suite rather than introducing a second test framework.

Boundary and registration checks live in `D:/Homeworks/c#/Quizapp/tests/Quizapp.Tests/Architecture`. Extend them when changing project references or service registration.

## Editing conventions

- Match neighboring C# files: file-scoped namespaces, four-space indentation, PascalCase public members, and `_camelCase` private fields. Preserve local encoding and line endings; avoid unrelated formatting churn.
- The repository's current formatting is the source of truth: keep code aligned with the existing style in the file, project, and solution. Do not introduce a different formatting standard, broader reflow, or formatter churn that changes unrelated code. Preserve the current layout, spacing, line endings, and surrounding conventions unless the task explicitly requires a format update.
- Nullable reference types and implicit usings are enabled. Follow existing `required`, `init`, and constructor patterns; preserve DTO serialization behavior when changing contracts.
- Use existing packages and abstractions. Inspect the affected `.csproj` before adding dependencies; avoid introducing a new architectural pattern for a small change.
- Shared field lengths belong in `D:/Homeworks/c#/Quizapp/Quizapp/Quizapp.Domain/Constants/FieldLimits.cs`. Keep DTO validators and EF column constraints consistent with those limits.
- Add validators alongside the matching DTO feature under Application. `AddApplication()` discovers validators by assembly scanning; verify new validators resolve through DI with scoped lifetime.
- There is no shared `Directory.Build.props` yet. Treat compiler warnings as potential issues and keep the build warning-free.
- The custom JetBrains `PublicAPI`/`UsedImplicitly` attributes and their imported source links were intentionally removed. Do not reintroduce them merely to suppress inspection warnings.
- Limit source searches to relevant projects. Exclude generated/build/tool directories such as `bin`, `obj`, and `.artifacts`; keep their contents out of source edits.

## Contract and persistence guardrails

For DTO changes, read `D:/Homeworks/c#/Quizapp/tests/Quizapp.Tests/Application/DtoContractTests.cs` and the related validation tests. Preserve these boundaries:

- Registration accepts profile data, not client-assigned roles or account status.
- Public user/authentication responses exclude passwords; quiz-taking responses exclude correct-answer flags.
- Quiz submissions do not accept client-supplied scores or user identity.

For entity or mapping changes, read the tests in `D:/Homeworks/c#/Quizapp/tests/Quizapp.Tests/Data` and the affected configuration in `D:/Homeworks/c#/Quizapp/Quizapp/Quizapp.Infrastructure/Persistence/Configurations`.

- Entity configurations are explicitly applied in `D:/Homeworks/c#/Quizapp/Quizapp/Quizapp.Infrastructure/Persistence/QuizAppDbContext.cs`. Register new configurations there; creating a configuration class alone is insufficient.
- Preserve reusable questions and many-to-many role assignments. Review delete behavior for retained attempt history and SQL Server multiple-cascade-path restrictions.
- Preserve the option/question composite foreign key, filtered uniqueness constraints, and option-versus-text response constraint unless the requested behavior explicitly changes them.
- Generate schema migrations with EF tooling in Infrastructure and Api as the startup project. Review the migration and model snapshot together; preserve existing migration history. Model tests check for pending model changes.

## Database and secret safety

- Use `ConnectionStrings__DefaultConnection` for local API overrides and `QUIZAPP_TEST_SQLSERVER_CONNECTION_STRING` for opt-in SQL Server tests. Keep values out of logs, documentation, and commits.
- Run database-mutating commands only against a verified development/test target. A connection setting already present in the environment is not evidence that its target is safe.
- SQL tests create, migrate, and drop unique `QuizappRelationTests_<guid>` databases. Use a dedicated test instance and an account permitted to create/drop databases; never target production. Read `D:/Homeworks/c#/Quizapp/tests/Quizapp.Tests/Data/SqlServerFixture.cs` before changing fixture cleanup.
- If no safe test connection is available, run database-independent tests and explicitly report skipped integration coverage. Do not replace SQL Server constraint tests with an in-memory provider and claim equivalent coverage.

## Verification workflow

Use PowerShell and quote paths, including the `c#` directory. Adjust the root for a different checkout:

```powershell
$RepoRoot = 'D:/Homeworks/c#/Quizapp'
$Solution = "$RepoRoot/Quizapp.sln"
$Tests = "$RepoRoot/tests/Quizapp.Tests/Quizapp.Tests.csproj"

dotnet restore "$Solution"
dotnet build "$Solution" --no-restore
# Run only after a successful build:
dotnet test "$Tests" --no-build --no-restore
```

- For behavioral fixes, add a regression test at the affected contract/validation/model/persistence boundary and verify it fails before the fix where practical.
- For a focused test run, append `--filter 'FullyQualifiedName~RequestValidationTests'` (substitute the relevant class). Finish with the full suite for code changes.
- `--no-build` uses previously compiled tests. Rebuild after source changes and match configurations when testing Release output.
- For HTTP changes, smoke-test the actual route, status, and response. A successful build or an empty Swagger document does not validate business behavior.
- For documentation-only changes, check referenced paths and commands against the current repository and reread the completed file.
- Review all edited files and the final diff. Report what changed, the commands actually run, pass/fail results, and any skipped or unverified checks. Do not record fixed test counts here; obtain current results from the test runner.
