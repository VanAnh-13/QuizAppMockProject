import {TestBed} from '@angular/core/testing';
import {ApiClient} from '../../../core/api/api-client';
import {ApiQuizAttempt} from './api-quiz-attempt';

describe('ApiQuizAttempt', () => {
    const result = {
        id: 'attempt-id',
        quizId: 'quiz-id',
        quizTitle: 'Quiz title',
        submittedAt: '2024-05-24T14:35:00Z',
        score: 80,
        passedScore: 70,
    };

    function setup(payload: unknown) {
        const api = {
            get: vi.fn().mockResolvedValue(payload),
            list: vi.fn().mockResolvedValue([payload]),
        };
        TestBed.configureTestingModule({
            providers: [ApiQuizAttempt, {provide: ApiClient, useValue: api}],
        });
        return {attempts: TestBed.inject(ApiQuizAttempt), api};
    }

    it.each([
        null,
        {...result, id: 1},
        {...result, quizId: null},
        {...result, quizTitle: null},
        {...result, submittedAt: 'invalid'},
        {...result, score: NaN},
    ])('rejects invalid results in both history modes: %s', async (payload) => {
        const {attempts, api} = setup(payload);
        api.get.mockResolvedValue({items: [payload], totalCount: 1, pageNumber: 1, pageSize: 8});

        await expect(attempts.history('quiz-id')).rejects.toThrow('Invalid attempt result.');
        await expect(attempts.historyPage(1, 8)).rejects.toThrow('Invalid attempt result.');
    });

    it.each([null, undefined, NaN, Infinity])('normalizes a missing or invalid pass mark in both history modes: %s', async (passedScore) => {
        const payload = {...result, passedScore};
        const {attempts, api} = setup(payload);
        api.get.mockResolvedValue({items: [payload], totalCount: 1, pageNumber: 1, pageSize: 8});

        expect((await attempts.history('quiz-id'))[0]?.passedScore).toBeNull();
        expect((await attempts.historyPage(1, 8)).items[0]?.passedScore).toBeNull();
    });

    it('keeps an empty page and falls back to requested pagination when metadata is absent', async () => {
        const {attempts} = setup({items: [], totalCount: 0});

        expect(await attempts.historyPage(1, 8)).toEqual({
            items: [], totalCount: 0, pageNumber: 1, pageSize: 8,
        });
    });

    it('requests a page of submitted attempts', async () => {
        const entry = {
            id: 'attempt-id',
            quizId: 'quiz-id',
            quizTitle: 'C# Căn bản',
            submittedAt: '2024-05-24T14:35:00Z',
            score: 82.5,
            passedScore: 70,
        };
        const {attempts, api} = setup({items: [entry], totalCount: 12, pageNumber: 2, pageSize: 8});

        const page = await attempts.historyPage(2, 8);

        expect(api.get).toHaveBeenCalledWith('quiz-history', {pageNumber: 2, pageSize: 8});
        expect(page).toEqual({items: [entry], totalCount: 12, pageNumber: 2, pageSize: 8});
    });

    it('treats a missing pass mark as not configured', async () => {
        const {attempts} = setup({
            items: [
                {
                    id: 'attempt-id',
                    quizId: 'quiz-id',
                    quizTitle: 'C# Căn bản',
                    submittedAt: '2024-05-24T14:35:00Z',
                    score: 65,
                    passedScore: null,
                },
            ],
            totalCount: 1,
            pageNumber: 1,
            pageSize: 8,
        });

        const page = await attempts.historyPage(1, 8);

        expect(page.items[0]?.passedScore).toBeNull();
    });

    it.each([
        {items: 'not-an-array', totalCount: 1, pageNumber: 1, pageSize: 8},
        {items: [], totalCount: -1, pageNumber: 1, pageSize: 8},
    ])('rejects an invalid history page %s', async (payload) => {
        const {attempts} = setup(payload);

        await expect(attempts.historyPage(1, 8)).rejects.toThrow(
            'Invalid attempt history page.',
        );
    });

    it('rejects an attempt row without a submission timestamp', async () => {
        const {attempts} = setup({
            items: [{id: 'a', quizId: 'q', quizTitle: 'T', submittedAt: 'not-a-date', score: 1}],
            totalCount: 1,
            pageNumber: 1,
            pageSize: 8,
        });

        await expect(attempts.historyPage(1, 8)).rejects.toThrow('Invalid attempt result.');
    });

    it('uses the protected attempt endpoints and revision payloads', async () => {
        const api = {
            get: vi.fn().mockResolvedValue({}),
            post: vi.fn().mockResolvedValue({}),
            put: vi.fn().mockResolvedValue({}),
            list: vi.fn().mockResolvedValue([]),
        };
        TestBed.configureTestingModule({
            providers: [ApiQuizAttempt, {provide: ApiClient, useValue: api}],
        });
        const attempts = TestBed.inject(ApiQuizAttempt);
        const answer = {questionId: 'question-id', answerIds: ['answer-id']};

        await attempts.start('quiz-id');
        await attempts.save('attempt-id', 2, [answer]);
        await attempts.pause('attempt-id', 3, [answer]);
        await attempts.resume('attempt-id', 4);
        await attempts.submit('attempt-id', 5);
        await attempts.history('quiz-id');

        expect(api.post).toHaveBeenCalledWith('quizzes/quiz-id/start');
        expect(api.put).toHaveBeenCalledWith('attempts/attempt-id/progress', {
            revision: 2,
            answers: [answer],
        });
        expect(api.post).toHaveBeenCalledWith('attempts/attempt-id/pause', {
            revision: 3,
            answers: [answer],
        });
        expect(api.post).toHaveBeenCalledWith('attempts/attempt-id/resume', {revision: 4});
        expect(api.post).toHaveBeenCalledWith('attempts/attempt-id/submit', {revision: 5});
        expect(api.list).toHaveBeenCalledWith('quiz-history', {quizId: 'quiz-id'});
    });
});
