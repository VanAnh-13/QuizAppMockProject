export const QuestionType = {
    MultipleChoice: 1,
    TrueFalse: 2,
    SingleChoice: 3,
    FillInTheBlanks: 4,
    ShortAnswer: 5,
    LongAnswer: 6,
} as const;

export type QuestionType = (typeof QuestionType)[keyof typeof QuestionType];

export const QuestionLevel = {
    Easy: 1,
    Medium: 2,
    Hard: 3,
} as const;

export type QuestionLevel = (typeof QuestionLevel)[keyof typeof QuestionLevel];

export interface AdminQuestion {
    readonly id: string;
    readonly content: string;
    readonly image: string | null;
    readonly level: QuestionLevel | null;
    readonly questionType: QuestionType;
    readonly isActive: boolean;
}

export interface AdminQuestionPage {
    readonly items: readonly AdminQuestion[];
    readonly totalCount: number;
    readonly pageNumber: number;
    readonly pageSize: number;
}

export interface AdminQuestionAnswerDraft {
    readonly text: string;
    readonly isCorrect: boolean;
    readonly isActive: boolean;
}

export interface AdminQuestionDraft {
    readonly content: string;
    readonly image: string | null;
    readonly level: QuestionLevel;
    readonly questionType: QuestionType;
    readonly isActive: boolean;
    readonly answers: readonly AdminQuestionAnswerDraft[];
}

const QUESTION_TYPE_LABELS: Record<QuestionType, string> = {
    [QuestionType.MultipleChoice]: 'Multiple choice',
    [QuestionType.TrueFalse]: 'True / False',
    [QuestionType.SingleChoice]: 'Single choice',
    [QuestionType.FillInTheBlanks]: 'Fill in the blanks',
    [QuestionType.ShortAnswer]: 'Short answer',
    [QuestionType.LongAnswer]: 'Long answer',
};

const QUESTION_TYPE_ICONS: Record<QuestionType, string> = {
    [QuestionType.MultipleChoice]: 'check_box',
    [QuestionType.TrueFalse]: 'flaky',
    [QuestionType.SingleChoice]: 'radio_button_checked',
    [QuestionType.FillInTheBlanks]: 'edit_note',
    [QuestionType.ShortAnswer]: 'short_text',
    [QuestionType.LongAnswer]: 'notes',
};

const QUESTION_LEVEL_LABELS: Record<QuestionLevel, string> = {
    [QuestionLevel.Easy]: 'Easy',
    [QuestionLevel.Medium]: 'Medium',
    [QuestionLevel.Hard]: 'Hard',
};

export function questionTypeLabel(type: QuestionType): string {
    return QUESTION_TYPE_LABELS[type] ?? 'Unknown';
}

export function questionTypeIcon(type: QuestionType): string {
    return QUESTION_TYPE_ICONS[type] ?? 'help';
}

export function questionLevelLabel(level: QuestionLevel | null): string {
    return level === null ? 'Unrated' : (QUESTION_LEVEL_LABELS[level] ?? 'Unrated');
}

export function questionCode(id: string): string {
    return `QID-${id.replaceAll('-', '').slice(-4).toUpperCase()}`;
}
