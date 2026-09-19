import {inject, Injectable} from '@angular/core';
import {ApiClient, PagedResult} from '../../../core/api/api-client';
import {
    AttemptAnswer,
    AttemptApi,
    AttemptProgress,
    AttemptResult,
    AttemptStart,
    AttemptSummary,
} from '../application/attempt-api';

@Injectable({providedIn: 'root'})
export class ApiQuizAttempt implements AttemptApi {
    private readonly api = inject(ApiClient);

    start(quizId: string): Promise<AttemptStart> {
        return this.api.post(`quizzes/${encodeURIComponent(quizId)}/start`);
    }

    progress(id: string): Promise<AttemptProgress> {
        return this.api.get(this.path(id, 'progress'));
    }

    save(id: string, revision: number, answers: readonly AttemptAnswer[]): Promise<AttemptProgress> {
        return this.api.put(this.path(id, 'progress'), {revision, answers});
    }

    pause(id: string, revision: number, answers: readonly AttemptAnswer[]): Promise<AttemptProgress> {
        return this.api.post(this.path(id, 'pause'), {revision, answers});
    }

    resume(id: string, revision: number): Promise<AttemptProgress> {
        return this.api.post(this.path(id, 'resume'), {revision});
    }

    submit(id: string, revision: number): Promise<AttemptResult> {
        return this.api.post(this.path(id, 'submit'), {revision});
    }

    result(id: string): Promise<AttemptResult> {
        return this.api.get(this.path(id));
    }

    unfinished(quizId: string): Promise<readonly AttemptSummary[]> {
        return this.api.list('attempts/in-progress', {quizId});
    }

    async historyPage(pageNumber: number, pageSize: number): Promise<PagedResult<AttemptResult>> {
        const page = await this.api.get<PagedResult<AttemptResult>>('quiz-history', {
            pageNumber,
            pageSize,
        });

        if (
            !Array.isArray(page?.items) ||
            !Number.isInteger(page.totalCount) ||
            page.totalCount < 0
        ) {
            throw new Error('Invalid attempt history page.');
        }

        return {
            items: page.items.map(validateAttemptResult),
            totalCount: page.totalCount,
            pageNumber: Number.isInteger(page.pageNumber) ? page.pageNumber : pageNumber,
            pageSize: Number.isInteger(page.pageSize) ? page.pageSize : pageSize,
        };
    }

    async history(quizId: string): Promise<readonly AttemptResult[]> {
        return (await this.api.list<AttemptResult>('quiz-history', {quizId})).map(
            validateAttemptResult,
        );
    }

    private path(id: string, action?: string): string {
        return `attempts/${encodeURIComponent(id)}${action ? `/${action}` : ''}`;
    }
}

function validateAttemptResult(result: AttemptResult): AttemptResult {
    if (
        !result ||
        typeof result.id !== 'string' ||
        typeof result.quizId !== 'string' ||
        typeof result.quizTitle !== 'string' ||
        !Number.isFinite(Date.parse(result.submittedAt)) ||
        !Number.isFinite(result.score)
    ) {
        throw new Error('Invalid attempt result.');
    }
    return {...result, passedScore: Number.isFinite(result.passedScore) ? result.passedScore : null};
}
