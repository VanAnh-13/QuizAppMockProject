import {InjectionToken, Provider} from '@angular/core';
import {attemptContentProvider} from '../../quiz-attempt/infrastructure/attempt-content.provider';
import {QuizDetailsContent} from '../application/quiz-details-content';
import {ApiQuizDetailsContent} from './api-quiz-details-content';

export const QUIZ_DETAILS_CONTENT = new InjectionToken<QuizDetailsContent>('QUIZ_DETAILS_CONTENT');

export const quizDetailsContentProvider: Provider[] = [
    {provide: QUIZ_DETAILS_CONTENT, useClass: ApiQuizDetailsContent},
];

export function provideQuizDetails(): Provider[] {
    return [...quizDetailsContentProvider, attemptContentProvider];
}
