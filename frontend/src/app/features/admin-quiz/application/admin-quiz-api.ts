import {InjectionToken} from '@angular/core';
import {AdminQuiz, AdminQuizDraft, AdminQuizPage} from '../domain/admin-quiz';

export interface AdminQuizApi {
    list(pageNumber: number, pageSize: number, search: string): Promise<AdminQuizPage>;

    create(draft: AdminQuizDraft): Promise<AdminQuiz>;

    update(id: string, draft: AdminQuizDraft): Promise<void>;

    setActive(id: string, isActive: boolean): Promise<void>;
}

export const ADMIN_QUIZ_API = new InjectionToken<AdminQuizApi>('ADMIN_QUIZ_API');
export const ADMIN_QUIZ_CONFIG = new InjectionToken<{ pageSize: number }>('ADMIN_QUIZ_CONFIG', {
    providedIn: 'root',
    factory: () => ({pageSize: 6}),
});
