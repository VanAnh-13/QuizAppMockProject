import {inject, Injectable} from '@angular/core';
import {ApiClient} from '../../../core/api/api-client';
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
        throw new Error('Kết quả làm bài không hợp lệ.');
    }
    return {...result, passedScore: Number.isFinite(result.passedScore) ? result.passedScore : null};
}
