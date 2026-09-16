export type QuizCategoryId = 'csharp' | 'sql-server' | 'angular' | 'typescript' | 'api';

export interface QuizCategory {
    readonly id: QuizCategoryId;
    readonly label: string;
}

export interface QuizSummary {
    readonly id: string;
    readonly categoryId: QuizCategoryId | 'general';
    readonly categoryLabel: string;
    readonly title: string;
    readonly description: string;
    readonly questionCount: number | null;
    readonly durationMinutes: number;
    readonly status: 'open';
    readonly imageUrl?: string | null;
}
