import {inject} from '@angular/core';
import {ActivatedRouteSnapshot, ResolveFn} from '@angular/router';
import {QuizDetailsSnapshot} from '../application/quiz-details-content';
import {QUIZ_DETAILS_CONTENT} from '../infrastructure/quiz-details-content.provider';

export const quizDetailsResolver: ResolveFn<QuizDetailsSnapshot | null> = async (
    route: ActivatedRouteSnapshot,
) => {
    const quizId = route.paramMap.get('quizId');
    if (!quizId) return null;

    try {
        const content = inject(QUIZ_DETAILS_CONTENT);
        return await content.load(quizId);
    } catch {
        return null;
    }
};
