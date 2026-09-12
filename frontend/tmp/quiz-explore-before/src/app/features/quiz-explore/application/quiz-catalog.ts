import { QuizCategory, QuizSummary } from '../domain/quiz-summary';

export interface QuizCatalog {
  listCategories(): readonly QuizCategory[];
  listQuizzes(): readonly QuizSummary[];
}
