import {TestBed} from '@angular/core/testing';
import {ApiClient} from '../../../core/api/api-client';
import {QuestionLevel, QuestionType} from '../domain/admin-question';
import {ApiQuestionBank} from './api-question-bank';

describe('ApiQuestionBank', () => {
    it('loads and maps a paged question list', async () => {
        const get = vi.fn().mockResolvedValue({
            items: [
                {
                    id: '10000000-0000-0000-0000-000000000049',
                    content: 'Trong C#, kiểu dữ liệu nào sau đây là kiểu tham chiếu?',
                    image: null,
                    level: 1,
                    questionType: 3,
                    isActive: true,
                },
            ],
            totalCount: 1,
            pageNumber: 1,
            pageSize: 6,
        });
        TestBed.configureTestingModule({
            providers: [ApiQuestionBank, {provide: ApiClient, useValue: {get}}],
        });

        const page = await TestBed.inject(ApiQuestionBank).list(1, 6, 'C#');

        expect(get).toHaveBeenCalledWith('questions', {pageNumber: 1, pageSize: 6, search: 'C#'});
        expect(page).toEqual({
            items: [
                {
                    id: '10000000-0000-0000-0000-000000000049',
                    content: 'Trong C#, kiểu dữ liệu nào sau đây là kiểu tham chiếu?',
                    image: null,
                    level: QuestionLevel.Easy,
                    questionType: QuestionType.SingleChoice,
                    isActive: true,
                },
            ],
            totalCount: 1,
            pageNumber: 1,
            pageSize: 6,
        });
    });

    it('patches question activation', async () => {
        const patch = vi.fn().mockResolvedValue(undefined);
        TestBed.configureTestingModule({
            providers: [ApiQuestionBank, {provide: ApiClient, useValue: {patch}}],
        });

        await TestBed.inject(ApiQuestionBank).setActive('question-id', false);

        expect(patch).toHaveBeenCalledWith('questions/question-id/activate', {isActive: false});
    });

    it('creates a question with answers and updates stem metadata only', async () => {
        const payload = {
            id: '10000000-0000-0000-0000-000000000049',
            content: 'Trong C#, kiểu dữ liệu nào sau đây là kiểu tham chiếu?',
            image: null,
            level: 1,
            questionType: 3,
            isActive: true,
        };
        const post = vi.fn().mockResolvedValue(payload);
        const put = vi.fn().mockResolvedValue(undefined);
        TestBed.configureTestingModule({
            providers: [ApiQuestionBank, {provide: ApiClient, useValue: {post, put}}],
        });
        const api = TestBed.inject(ApiQuestionBank);
        const draft = {
            content: payload.content,
            image: null,
            level: QuestionLevel.Easy,
            questionType: QuestionType.SingleChoice,
            isActive: true,
            answers: [
                {text: 'class', isCorrect: true, isActive: true},
                {text: 'int', isCorrect: false, isActive: true},
            ],
        };

        await api.create(draft);
        await api.update(payload.id, draft);

        expect(post).toHaveBeenCalledWith(
            'questions',
            expect.objectContaining({answers: draft.answers}),
        );
        expect(put).toHaveBeenCalledWith(
            `questions/${payload.id}`,
            expect.not.objectContaining({answers: expect.anything()}),
        );
    });
});
