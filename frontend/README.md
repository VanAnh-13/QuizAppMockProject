# Quizapp frontend

Angular 22 client for quiz discovery, authentication, timed attempts, saved progress, submission, and attempt history.

## API integration

The browser calls relative `/api` URLs. During local development, Angular proxies those requests to the backend origin in `QUIZAPP_API_TARGET`; no server URL is embedded in application code.

Public screens use `GET /api/public/quizzes`. Authentication uses `/api/auth/register` and `/api/auth/login`. Attempt creation, progress, pause/resume, submission, results, and history use the protected routes documented in [the backend attempt contract](../backend/docs/attempt-progress.md).

## Run locally

Start the backend HTTP profile, then run:

```powershell
Set-Location "D:/Homeworks/angular/quiz_app/frontend"
pnpm install
$env:QUIZAPP_API_TARGET = 'http://localhost:5269'
pnpm start
```

Open `http://localhost:4200`.

## Verify

```powershell
pnpm test -- --watch=false
pnpm build
```

The main routes are:

- `/` — public quiz catalog
- `/quiz/:quizId` — public quiz details and authenticated attempt history
- `/quiz/:quizId/attempt` — authenticated quiz attempt

JWTs are kept in session storage and attached only to requests under the configured API base path.
