import {QuizSummary} from '../domain/quiz-summary';

export interface QuizCatalog {
    listQuizzes(): Promise<readonly QuizSummary[]>;
}
