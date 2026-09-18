import {inject, Injectable} from '@angular/core';
import {ApiClient} from '../../../core/api/api-client';
import {PublicQuizSummaryDto, validatePublicQuiz} from '../../../core/api/quiz-contracts';
import {QuizCatalog} from '../application/quiz-catalog';
import {QuizSummary} from '../domain/quiz-summary';

import {resolveQuizImageUrl} from '../../../core/utils/quiz-image';

@Injectable()
export class ApiQuizCatalog implements QuizCatalog {
    private readonly api = inject(ApiClient);

    async listQuizzes(): Promise<readonly QuizSummary[]> {
        return (await this.api.list<PublicQuizSummaryDto>('public/quizzes'))
            .map(validatePublicQuiz)
            .map((quiz) => ({
                id: quiz.id,
                title: quiz.title,
                description: quiz.description ?? '',
                durationMinutes: quiz.duration,
                categoryId: 'general',
                categoryLabel: 'General knowledge',
                questionCount: quiz.questionCount,
                status: 'open',
                imageUrl: resolveQuizImageUrl(quiz.title, quiz.imageUrl),
            }));
    }
}
