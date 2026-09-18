import {Provider} from '@angular/core';
import {QUESTION_BANK_API} from '../application/question-bank-api';
import {ApiQuestionBank} from './api-question-bank';

export function provideQuestionBank(): Provider[] {
    return [{provide: QUESTION_BANK_API, useClass: ApiQuestionBank}];
}
