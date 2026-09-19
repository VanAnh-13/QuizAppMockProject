import {inject, Injectable} from '@angular/core';
import {ApiClient, PagedResult} from '../../../core/api/api-client';
import {listParams, validatePagedResult} from '../../../core/api/paged-result';
import {AdminQuizApi} from '../application/admin-quiz-api';
import {AdminQuiz, AdminQuizDraft, AdminQuizPage} from '../domain/admin-quiz';

interface QuizPayload {
    readonly id: string;
    readonly title: string;
    readonly description?: string | null;
    readonly duration: number;
    readonly image?: string | null;
    readonly passedScore?: number | null;
    readonly isActive: boolean;
}

@Injectable()
export class ApiAdminQuiz implements AdminQuizApi {
    private readonly api = inject(ApiClient);

    async list(pageNumber: number, pageSize: number, search: string): Promise<AdminQuizPage> {
        const page = validatePagedResult(
            await this.api.get<PagedResult<QuizPayload>>('quizzes', listParams(pageNumber, pageSize, search)),
            pageNumber,
            pageSize,
            'Invalid quiz data.',
        );
        return {...page, items: page.items.map(validateQuiz)};
    }

    async create(draft: AdminQuizDraft): Promise<AdminQuiz> {
        return validateQuiz(await this.api.post<QuizPayload>('quizzes', draft));
    }

    async update(id: string, draft: AdminQuizDraft): Promise<void> {
        await this.api.put<void>(`quizzes/${id}`, draft);
    }

    async setActive(id: string, isActive: boolean): Promise<void> {
        await this.api.patch<void>(`quizzes/${id}/activate`, {isActive});
    }
}

function validateQuiz(quiz: QuizPayload): AdminQuiz {
    if (!quiz || typeof quiz.id !== 'string' || typeof quiz.title !== 'string' || !Number.isFinite(quiz.duration)) {
        throw new Error('Invalid quiz.');
    }
    return {
        id: quiz.id,
        title: quiz.title,
        description: quiz.description ?? null,
        duration: quiz.duration,
        image: quiz.image ?? null,
        passedScore: Number.isFinite(quiz.passedScore) ? quiz.passedScore! : null,
        isActive: Boolean(quiz.isActive),
    };
}
