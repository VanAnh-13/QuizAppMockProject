import {Provider} from '@angular/core';
import {ADMIN_QUIZ_API} from '../application/admin-quiz-api';
import {ApiAdminQuiz} from './api-admin-quiz';

export function provideAdminQuiz(): Provider[] {
    return [{provide: ADMIN_QUIZ_API, useClass: ApiAdminQuiz}];
}
