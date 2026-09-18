export interface AdminQuiz {
    readonly id: string;
    readonly title: string;
    readonly description: string | null;
    readonly duration: number;
    readonly image: string | null;
    readonly passedScore: number | null;
    readonly isActive: boolean;
}

export interface AdminQuizPage {
    readonly items: readonly AdminQuiz[];
    readonly totalCount: number;
    readonly pageNumber: number;
    readonly pageSize: number;
}

export interface AdminQuizDraft {
    readonly title: string;
    readonly description: string | null;
    readonly duration: number;
    readonly image: string | null;
    readonly passedScore: number;
    readonly isActive: boolean;
}
