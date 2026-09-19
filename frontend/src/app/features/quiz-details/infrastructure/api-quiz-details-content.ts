import {inject, Injectable} from '@angular/core';
import {ApiClient} from '../../../core/api/api-client';
import {PublicQuizSummaryDto, validatePublicQuiz} from '../../../core/api/quiz-contracts';
import {QuizDetailsContent, QuizDetailsSnapshot} from '../application/quiz-details-content';

import {resolveQuizImageUrl} from '../../../core/utils/quiz-image';

@Injectable()
export class ApiQuizDetailsContent implements QuizDetailsContent {
    private readonly api = inject(ApiClient);

    async load(quizId: string): Promise<QuizDetailsSnapshot> {
        const quizzes = (await this.api.list<PublicQuizSummaryDto>('public/quizzes')).map(
            validatePublicQuiz,
        );

        const quiz = quizzes.find((item) => item.id === quizId);

        if (!quiz) throw new Error('This quiz is not available.');
        return {
            imageUrl: resolveQuizImageUrl(
                quiz.title,
                quiz.imageUrl,
            ),
            title: quiz.title,
            description: quiz.description ?? '',
            categoryLabel: 'Quiz',
            metrics: [
                {icon: 'quiz', label: 'Questions', value: `${quiz.questionCount} questions`},
                {icon: 'timer', label: 'Duration', value: `${quiz.duration} minutes`},
                {
                    icon: 'military_tech',
                    label: 'Passing score',
                    value: quiz.passedScore === null ? 'Not specified' : `${quiz.passedScore}%`,
                },
            ],
            topics: [],
            guidelines: [
                {
                    title: 'Saving progress',
                    description: 'Answers are saved automatically. Check the save status before leaving the page.',
                },
                {
                    title: 'Pause and resume',
                    description:
                        'Pausing saves your answers and stops the timer once the server confirms. Closing the page does not pause the timer.',
                },
                {
                    title: 'Submit quiz',
                    description:
                        'When time runs out, your saved answers are submitted. Answers cannot be changed after submission.',
                },
            ],
            formatFacts: [{label: 'Status', value: 'Open'}],
        };
    }
}
