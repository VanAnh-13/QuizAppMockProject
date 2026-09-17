import {Routes} from '@angular/router';
import {leaveAttemptGuard} from './features/quiz-attempt/presentation/leave-attempt.guard';
import {provideQuizDetails} from './features/quiz-details/infrastructure/quiz-details-content.provider';
import {quizDetailsResolver} from './features/quiz-details/presentation/quiz-details.resolver';
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
        path: 'history',
        title: 'Lịch sử làm bài — QuizApp',
        canActivate: [requireAuthGuard],
        loadComponent: () =>
            import('./features/account/presentation/account-history.page').then(
                (m) => m.AccountHistoryPage,
            ),
    },
    {
        path: 'settings',
        title: 'Cài đặt tài khoản & Bảo mật — QuizApp',
        canActivate: [requireAuthGuard],
        loadComponent: () =>
            import('./features/account/presentation/account-settings.page').then(
                (m) => m.AccountSettingsPage,
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
        providers: [provideQuizDetails()],
        resolve: {snapshot: quizDetailsResolver},
        loadComponent: () =>
            import('./features/quiz-details/presentation/quiz-details.page').then(
                (m) => m.QuizDetailsPage,
            ),
    },
    {path: '**', redirectTo: ''},
];
