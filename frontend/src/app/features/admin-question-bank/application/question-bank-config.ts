import {InjectionToken} from '@angular/core';

export interface QuestionBankConfig {
    readonly pageSize: number;
}

export const QUESTION_BANK_CONFIG = new InjectionToken<QuestionBankConfig>('QUESTION_BANK_CONFIG', {
    providedIn: 'root',
    factory: () => ({pageSize: 6}),
});
