import {InjectionToken, Provider} from '@angular/core';
import {QuizDetailsContent} from '../application/quiz-details-content';
import {ApiQuizDetailsContent} from './api-quiz-details-content';

export const QUIZ_DETAILS_CONTENT = new InjectionToken<QuizDetailsContent>('QUIZ_DETAILS_CONTENT');

export const quizDetailsContentProvider: Provider[] = [
    {provide: QUIZ_DETAILS_CONTENT, useClass: ApiQuizDetailsContent},
];
