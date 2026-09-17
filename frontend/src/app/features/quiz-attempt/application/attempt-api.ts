import {InjectionToken} from '@angular/core';

export interface AttemptAnswer {
    readonly questionId: string;
    readonly answerIds: readonly string[];
    readonly responseText?: string | null;
}

export interface AttemptQuestionDto {
    readonly id: string;
    readonly content: string;
    readonly image?: string | null;
    readonly questionType: number;
    readonly order: number;
    readonly answers: readonly { readonly id: string; readonly text: string }[];
}

export interface AttemptStart {
    readonly attemptId: string;
    readonly quiz: {
        readonly quizId: string;
        readonly title: string;
        readonly questions: readonly AttemptQuestionDto[];
    };
    readonly revision: number;
    readonly startedAt: string;
    readonly expiresAt: string;
    readonly serverTime: string;
}

export interface AttemptProgress extends AttemptStart {
    readonly pausedAt: string | null;
    readonly lastSavedAt: string | null;
    readonly remainingSeconds: number;
    readonly answers: readonly AttemptAnswer[];
}

export interface AttemptSummary {
    readonly attemptId: string;
    readonly quizId: string;
    readonly quizTitle: string;
    readonly pausedAt: string | null;
    readonly remainingSeconds: number;
}

export interface AttemptResult {
    readonly id: string;
    readonly quizId: string;
    readonly quizTitle: string;
    readonly submittedAt: string;
    readonly score: number;
    readonly passedScore: number | null;
}

export interface AttemptApi {
    start(quizId: string): Promise<AttemptStart>;

    progress(attemptId: string): Promise<AttemptProgress>;

    save(
        attemptId: string,
        revision: number,
        answers: readonly AttemptAnswer[],
    ): Promise<AttemptProgress>;

    pause(
        attemptId: string,
        revision: number,
        answers: readonly AttemptAnswer[],
    ): Promise<AttemptProgress>;

    resume(attemptId: string, revision: number): Promise<AttemptProgress>;

    submit(attemptId: string, revision: number): Promise<AttemptResult>;

    result(attemptId: string): Promise<AttemptResult>;

    unfinished(quizId: string): Promise<readonly AttemptSummary[]>;

    history(quizId: string): Promise<readonly AttemptResult[]>;
}

export const ATTEMPT_API = new InjectionToken<AttemptApi>('ATTEMPT_API');
