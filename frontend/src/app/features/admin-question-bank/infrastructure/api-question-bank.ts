import {inject, Injectable} from '@angular/core';
import {ApiClient, PagedResult} from '../../../core/api/api-client';
import {listParams, validatePagedResult} from '../../../core/api/paged-result';
import {QuestionBankApi} from '../application/question-bank-api';
import {
    AdminQuestion,
    AdminQuestionDraft,
    AdminQuestionPage,
    QuestionLevel,
    QuestionType,
} from '../domain/admin-question';

interface QuestionPayload {
    readonly id: string;
    readonly content: string;
    readonly image?: string | null;
    readonly level?: number | null;
    readonly questionType: number;
    readonly isActive: boolean;
}

@Injectable()
export class ApiQuestionBank implements QuestionBankApi {
    private readonly api = inject(ApiClient);

    async list(pageNumber: number, pageSize: number, search: string): Promise<AdminQuestionPage> {
        const page = validatePagedResult(
            await this.api.get<PagedResult<QuestionPayload>>(
                'questions',
                listParams(pageNumber, pageSize, search),
            ),
            pageNumber,
            pageSize,
            'Question bank data is invalid.',
        );

        return {...page, items: page.items.map(validateQuestion)};
    }

    async get(id: string): Promise<AdminQuestion> {
        return validateQuestion(await this.api.get<QuestionPayload>(`questions/${id}`));
    }

    async create(draft: AdminQuestionDraft): Promise<AdminQuestion> {
        return validateQuestion(
            await this.api.post<QuestionPayload>('questions', questionBody(draft, true)),
        );
    }

    async update(id: string, draft: AdminQuestionDraft): Promise<void> {
        await this.api.put<void>(`questions/${id}`, questionBody(draft, false));
    }

    async setActive(id: string, isActive: boolean): Promise<void> {
        await this.api.patch<void>(`questions/${id}/activate`, {isActive});
    }
}

function questionBody(draft: AdminQuestionDraft, withAnswers: boolean) {
    return {
        content: draft.content,
        image: draft.image,
        level: draft.level,
        questionType: draft.questionType,
        isActive: draft.isActive,
        ...(withAnswers
            ? {
                answers: draft.answers.map((answer) => ({
                    text: answer.text,
                    isCorrect: answer.isCorrect,
                    isActive: answer.isActive,
                })),
            }
            : {}),
    };
}

function validateQuestion(question: QuestionPayload): AdminQuestion {
    if (
        !question ||
        typeof question.id !== 'string' ||
        typeof question.content !== 'string' ||
        !isQuestionType(question.questionType) ||
        typeof question.isActive !== 'boolean'
    ) {
        throw new Error('Invalid question.');
    }

    return {
        id: question.id,
        content: question.content,
        image: typeof question.image === 'string' ? question.image : null,
        level: isQuestionLevel(question.level) ? question.level : null,
        questionType: question.questionType,
        isActive: question.isActive,
    };
}

function isQuestionType(value: number): value is QuestionType {
    return (
        value === QuestionType.MultipleChoice ||
        value === QuestionType.TrueFalse ||
        value === QuestionType.SingleChoice ||
        value === QuestionType.FillInTheBlanks ||
        value === QuestionType.ShortAnswer ||
        value === QuestionType.LongAnswer
    );
}

function isQuestionLevel(value: number | null | undefined): value is QuestionLevel {
    return value === QuestionLevel.Easy || value === QuestionLevel.Medium || value === QuestionLevel.Hard;
}
