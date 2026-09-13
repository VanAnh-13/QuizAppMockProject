import { CanDeactivateFn } from '@angular/router';
import { QuizAttemptPage } from './quiz-attempt.page';

export const leaveAttemptGuard: CanDeactivateFn<QuizAttemptPage> = (page) => page.canLeave();
