# Frontend agent instructions

These instructions apply to everything under `frontend/`. They extend the repository-level
`AGENTS.md`; follow both files. If the rules conflict, this file controls frontend work.

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

HTML templates and all other file types use different formatters:

- HTML templates (`*.html`) follow the IDE code style in `.editorconfig`, with
  `src/app/features/quiz-attempt/presentation/quiz-attempt.page.html` as the canonical example:
  four-space indentation, eight-space continuation indent, attributes wrapped when a line exceeds
  100 characters, and the closing `>` of a multi-line tag on its own line. Format HTML with the
  IDE's Reformat Code action, which reads `.editorconfig` directly. Do not run Prettier on HTML;
  `**/*.html` is listed in `.prettierignore`. Inline templates inside TypeScript files stay with
  the TypeScript rules below.
- TypeScript, CSS, JSON, and Markdown follow `.prettierrc`: two-space indentation, single quotes,
  and a 100-character print width.

TypeScript style details:

- Put spaces inside import and object braces: `{ inject, Injectable }`.
- Never place multiple properties, statements, methods, or template blocks on one line.
- Put each method body on multiple lines when it contains logic or a conditional.
- Add a blank line between the import block and the first declaration.
- Add a blank line between interfaces, types, classes, and top-level functions.
- In classes, keep related fields together, then add a blank line before the first method.
- Keep a blank line between methods and between distinct logical blocks inside a method.
- Split long parameter lists, object types, object literals, and chained expressions across lines.
- Do not reformat generated files or unrelated files.

Format every touched TypeScript and CSS file before completion:

```powershell
.\node_modules\.bin\prettier.cmd --write "src/path/to/touched-file.ts"
.\node_modules\.bin\prettier.cmd --check "src/**/*.{ts,css}"
```

Do not report formatting as complete while the Prettier check reports warnings.

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
- Preserve Vietnamese UI text and accessibility behavior unless the request explicitly changes it.

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
