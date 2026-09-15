import {Routes} from '@angular/router';
import {leaveAttemptGuard} from './features/quiz-attempt/presentation/leave-attempt.guard';
import {requireAuthGuard} from './core/auth/require-auth.guard';

export const routes: Routes = [
    {
        path: 'login',
        title: 'Đăng nhập — QuizApp',
        loadComponent: () => import('./features/auth/presentation/auth.page').then((m) => m.AuthPage),
    },
    {
        path: 'register',
        title: 'Tạo tài khoản — QuizApp',
        data: {registering: true},
        loadComponent: () => import('./features/auth/presentation/auth.page').then((m) => m.AuthPage),
    },
    {
        path: '',
        loadComponent: () =>
            import('./features/quiz-explore/presentation/quiz-explore.page').then(
                (m) => m.QuizExplorePage,
            ),
    },
    {
        path: 'quiz/:quizId/attempt',
        canActivate: [requireAuthGuard],
        loadComponent: () =>
            import('./features/quiz-attempt/presentation/quiz-attempt.page').then(
                (m) => m.QuizAttemptPage,
            ),
        canDeactivate: [leaveAttemptGuard],
    },
    {
        path: 'quiz/:quizId',
        loadComponent: () =>
            import('./features/quiz-details/presentation/quiz-details.page').then(
                (m) => m.QuizDetailsPage,
            ),
    },
    {path: '**', redirectTo: ''},
];
