export interface PublicQuizSummaryDto {
    readonly id: string;
    readonly title: string;
    readonly description: string | null;
    readonly duration: number;
    readonly imageUrl?: string | null;
    readonly passedScore: number | null;
    readonly questionCount: number;
    readonly updatedAt: string;
}

export function validatePublicQuiz(quiz: PublicQuizSummaryDto): PublicQuizSummaryDto {
    if (
        !quiz ||
        typeof quiz.id !== 'string' ||
        typeof quiz.title !== 'string' ||
        !Number.isFinite(quiz.duration) ||
        !Number.isInteger(quiz.questionCount) ||
        quiz.questionCount < 0 ||
        !Number.isFinite(Date.parse(quiz.updatedAt))
    ) {
        throw new Error('Invalid quiz data.');
    }
    return quiz;
}
