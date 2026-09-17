import {inject} from '@angular/core';
import {ActivatedRouteSnapshot, ResolveFn} from '@angular/router';
import {QuizDetailsSnapshot} from '../application/quiz-details-content';
import {QUIZ_DETAILS_CONTENT} from '../infrastructure/quiz-details-content.provider';

export type QuizDetailsResolution = QuizDetailsSnapshot | { readonly errorMessage: string };

export const quizDetailsResolver: ResolveFn<QuizDetailsResolution> = async (
    route: ActivatedRouteSnapshot,
) => {
    const quizId = route.paramMap.get('quizId');
    if (!quizId) return {errorMessage: 'Không tìm thấy mã quiz trên đường dẫn.'};

    try {
        const content = inject(QUIZ_DETAILS_CONTENT);
        return await content.load(quizId);
    } catch {
        return {errorMessage: 'Không thể tải chi tiết quiz từ máy chủ. Vui lòng thử lại.'};
    }
};
