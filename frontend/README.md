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
- `/login` — sign in, optional remembered session, and guest browsing
- `/register` — create an account, then return to sign in
- `/quiz/:quizId` — public quiz details and authenticated attempt history
- `/quiz/:quizId/attempt` — authenticated quiz attempt

JWTs are kept in session storage by default. Selecting “Ghi nhớ đăng nhập” uses local storage until token expiry or logout. They are attached only to requests under the configured API base path. Passwords are never stored by the frontend.

The authentication pages follow the supplied Vietnamese designs. Registration accepts required name, username, email, and matching passwords, plus optional phone number and date of birth. Password reset and official policy documents are not yet available; their controls explain that status. Existing quiz dialogs continue to use the same authentication adapter.
