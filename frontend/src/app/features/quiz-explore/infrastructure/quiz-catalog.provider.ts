import {InjectionToken, Provider} from '@angular/core';
import {QuizCatalog} from '../application/quiz-catalog';
import {ApiQuizCatalog} from './api-quiz-catalog';

export const QUIZ_CATALOG = new InjectionToken<QuizCatalog>('QUIZ_CATALOG');

export const quizCatalogProvider: Provider = {
    provide: QUIZ_CATALOG,
    useClass: ApiQuizCatalog,
};
