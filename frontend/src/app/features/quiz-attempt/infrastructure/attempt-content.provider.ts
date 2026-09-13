import { Provider } from '@angular/core';
import { ATTEMPT_API } from '../application/attempt-api';
import { ApiQuizAttempt } from './api-quiz-attempt';

export const attemptContentProvider: Provider = { provide: ATTEMPT_API, useClass: ApiQuizAttempt };
