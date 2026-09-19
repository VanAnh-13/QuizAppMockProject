import {InjectionToken} from '@angular/core';
import {AdminQuestion, AdminQuestionDraft, AdminQuestionPage} from '../domain/admin-question';

export interface QuestionBankApi {
    list(pageNumber: number, pageSize: number, search: string): Promise<AdminQuestionPage>;

    get(id: string): Promise<AdminQuestion>;

    create(draft: AdminQuestionDraft): Promise<AdminQuestion>;

    update(id: string, draft: AdminQuestionDraft): Promise<void>;

    setActive(id: string, isActive: boolean): Promise<void>;
}

export const QUESTION_BANK_API = new InjectionToken<QuestionBankApi>('QUESTION_BANK_API');
