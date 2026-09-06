# Quizapp — Frontend

Angular + TypeScript web client for the Quizapp quiz-management platform.

> **This project has not been implemented yet.** The directory is reserved for the future web client. See the [root README](../README.md) for the current project status.

## Planned Tech Stack

| Layer | Technology |
| --- | --- |
| Framework | [Angular](https://angular.dev/) |
| Language | TypeScript |
| UI Library | TBD (Angular Material / Tailwind CSS) |
| State Management | TBD (NgRx / Angular Signals) |
| HTTP Client | Angular HttpClient |
| Testing | Jasmine + Karma / Jest |

## Planned Features

- **Authentication** — Login, registration, JWT token management
- **Quiz Taking** — Browse quizzes, answer questions, view results
- **Quiz Management** — Create, edit, and publish quizzes (admin/instructor)
- **Question Bank** — Manage reusable questions across quizzes
- **User Management** — Profile editing, role-based access
- **Attempt History** — Review past quiz attempts and scores

## Getting Started (Future)

Once implemented, the expected workflow will be:

```bash
# Install dependencies
npm install

# Start development server
ng serve

# Run tests
ng test

# Build for production
ng build --configuration production
```

The frontend will communicate with the backend API at the URLs documented in the [backend README](../backend/README.md).

## Project Structure (Planned)

```
frontend/
├── src/
│   ├── app/
│   │   ├── core/          # Auth, guards, interceptors, global services
│   │   ├── shared/        # Reusable components, pipes, directives
│   │   ├── features/      # Feature modules (quiz, auth, admin, etc.)
│   │   └── app.config.ts
│   ├── assets/
│   ├── environments/
│   └── index.html
├── angular.json
├── package.json
├── tsconfig.json
└── README.md
```

## Contributing

Refer to the [root README](../README.md) for branching model and development guidelines. Frontend feature branches should be prefixed with `feature/web-`, for example `feature/web-quiz-list`.
