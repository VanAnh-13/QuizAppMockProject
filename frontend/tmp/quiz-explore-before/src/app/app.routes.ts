import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./features/quiz-explore/presentation/quiz-explore.page').then(
        ({ QuizExplorePage }) => QuizExplorePage,
      ),
    title: 'Khám phá quiz | QuizApp',
  },
  { path: '**', redirectTo: '' },
];
