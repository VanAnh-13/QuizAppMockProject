import {QuizFormatFact, QuizGuideline, QuizMetric, QuizTopic} from '../domain/quiz-details';

export interface QuizDetailsSnapshot {
    readonly isActive?: boolean;
    readonly title: string;
    readonly description: string;
    readonly categoryLabel: string;
    readonly metrics: readonly QuizMetric[];
    readonly topics: readonly QuizTopic[];
    readonly guidelines: readonly QuizGuideline[];
    readonly formatFacts: readonly QuizFormatFact[];
}

export interface QuizDetailsContent {
    load(quizSlug: string): Promise<QuizDetailsSnapshot>;
}
