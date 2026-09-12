import {Routes} from '@angular/router';
import {leaveAttemptGuard} from './features/quiz-attempt/presentation/leave-attempt.guard';

export const routes: Routes = [
    {
        path: '',
        loadComponent: () =>
            import('./features/quiz-explore/presentation/quiz-explore.page').then(
                (m) => m.QuizExplorePage,
            ),
    },
    {
        path: 'quiz/:quizId/attempt',
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
