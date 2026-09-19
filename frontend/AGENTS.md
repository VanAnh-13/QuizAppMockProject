# Frontend agent instructions

These instructions apply to everything under `frontend/`. They extend the repository-level
`AGENTS.md`; follow both files. If the rules conflict, this file controls frontend work.

## Design system

Before touching any CSS, HTML, or visual component, read `frontend/DESIGN.MD`.
It defines the **Liquid Glass** style and the **Blue Gradient** brand palette — the single source
of truth for colours, glass tokens, component layers, and prohibited patterns.

## Required workflow

1. Inspect the working tree and read the affected implementation and tests before editing.
   Preserve existing changes and untracked files.
2. Before creating, modifying, or refactoring source code, inspect the skills available in the
   current environment and invoke at least one relevant implementation skill. The skill must match
   the work, such as API design, code simplification, Angular implementation, or bug diagnosis.
3. Invoking a skill means using the environment's actual skill mechanism and following the loaded
   `SKILL.md`. Mentioning a skill without loading it does not satisfy this requirement.
4. Before creating or changing tests, or running test commands, invoke a relevant testing skill.
   An implementation skill does not replace the testing-skill requirement unless its instructions
   explicitly cover testing.
5. If no relevant skill is available or skill invocation fails, stop before coding or testing,
   explain the limitation, and ask the user how to proceed.
6. Keep changes scoped to the request. Review the final diff and report the skills invoked,
   commands run, and observed results.

## Formatting is mandatory

The current formatting already applied by the user in WebStorm is the source of truth for every
frontend source file, including HTML, CSS, and TypeScript.

- Before editing a file, inspect its current formatting and the neighboring files. Preserve that
  indentation, spacing, wrapping, blank-line placement, attribute layout, and line-ending style.
- After editing, run WebStorm's **Reformat Code** action on every touched HTML, CSS, and TypeScript
  file using the project's current WebStorm code-style settings.
- Do not run Prettier or another formatter on HTML, CSS, or TypeScript unless the user explicitly
  requests it. A formatter must never replace or override the current WebStorm formatting.
- Do not manually impose a different brace-spacing, indentation, wrapping, or blank-line convention
  from examples, personal preference, framework defaults, or a formatter configuration.
- Limit formatting to files changed for the current request. Do not reformat generated files,
  untouched files, or unrelated code.
- Before completion, review the final diff and confirm that each touched HTML, CSS, and TypeScript
  file still matches the existing WebStorm-formatted style around it.

## Angular and TypeScript conventions

- Every Angular component must keep TypeScript, template, and styles in separate sibling files.
  Use `templateUrl` with `.html` and `styleUrl` or `styleUrls` with `.css`; do not add inline
  `template`, `styles`, or `style` metadata.
- Keep strict TypeScript types. Do not introduce `any` to bypass compiler errors.
- Prefer Angular dependency injection, signals, reactive forms, and standalone components already
  used by the project.
- Keep HTTP access in infrastructure adapters through `ApiClient`; components must not construct
  backend URLs or call `HttpClient` directly.
- Keep API base URLs, timing values, and environment-specific settings in configuration. Do not
  hardcode server URLs, secrets, or unexplained numbers.
- Keep domain models independent of Angular and HTTP details.
- Keep presentation code focused on rendering and user interaction; place state transitions in
  stores and remote calls in API adapters.
- Reuse existing interfaces and injection tokens instead of creating duplicate access paths.
- Handle loading, empty, authentication, and error states explicitly. Do not swallow exceptions.
- Write every user-facing string in English: headings, labels, buttons, placeholders,
  empty states, validation, API fallbacks, page titles, and `aria-*` / `alt` text.
  Keep existing accessibility attributes; only change their language.

## Verification

After invoking the required testing skill, run the checks appropriate to the change. For source
changes, the minimum verification is:

```powershell
pnpm.cmd test --watch=false
pnpm.cmd build
git diff --check
```

- Add or update focused tests for changed behavior, then run the full frontend suite.
- A passing build does not replace behavioral tests.
- For route or API changes, verify the real route, request, response, and error behavior when a safe
  local backend is available. Report live integration coverage as skipped when it is unavailable.
- Never claim a check passed unless its command completed successfully in the current worktree.
