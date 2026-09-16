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

        if (!quiz) throw new Error('Không tìm thấy quiz đang mở.');
        return {
            imageUrl: resolveQuizImageUrl(
                quiz.title,
                (quiz as {imageUrl?: string | null}).imageUrl,
            ),
            title: quiz.title,
            description: quiz.description ?? '',
            categoryLabel: 'Quiz',
            metrics: [
                {icon: 'quiz', label: 'Số câu hỏi', value: `${quiz.questionCount} câu hỏi`},
                {icon: 'timer', label: 'Thời lượng', value: `${quiz.duration} phút`},
                {
                    icon: 'military_tech',
                    label: 'Điểm đạt',
                    value: quiz.passedScore === null ? 'Không quy định' : `${quiz.passedScore}%`,
                },
            ],
            topics: [],
            guidelines: [
                {
                    title: 'Lưu tiến độ',
                    description: 'Đáp án được lưu tự động. Theo dõi thông báo lưu trước khi rời trang.',
                },
                {
                    title: 'Tạm dừng và tiếp tục',
                    description:
                        'Nút tạm dừng lưu đáp án và dừng đồng hồ sau khi máy chủ xác nhận. Đóng trang không tạm dừng thời gian.',
                },
                {
                    title: 'Nộp bài',
                    description:
                        'Khi hết giờ, hệ thống nộp các đáp án đã lưu thành công. Không thể sửa đáp án sau khi nộp.',
                },
            ],
            formatFacts: [{label: 'Trạng thái', value: 'Đang mở'}],
        };
    }
}
