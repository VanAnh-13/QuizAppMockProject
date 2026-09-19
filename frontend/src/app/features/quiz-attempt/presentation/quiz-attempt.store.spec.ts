import {TestBed} from '@angular/core/testing';
import {ATTEMPT_API, AttemptApi, AttemptProgress, AttemptStart} from '../application/attempt-api';
import {ATTEMPT_CONFIG} from '../application/attempt-config';
import {QuizAttemptStore} from './quiz-attempt.store';

const quizId = '10000000-0000-0000-0000-000000000001';
const attemptId = '40000000-0000-0000-0000-000000000001';
const start: AttemptStart = {
    attemptId,
    quiz: {
        quizId,
        title: 'C# basics',
        questions: [
            {
                id: '20000000-0000-0000-0000-000000000001',
                content: 'Select the correct options',
                questionType: 1,
                order: 1,
                answers: [
                    {id: '30000000-0000-0000-0000-000000000001', text: 'First'},
                    {id: '30000000-0000-0000-0000-000000000002', text: 'Second'},
                ],
            },
            {
                id: '20000000-0000-0000-0000-000000000002',
                content: 'Explain the answer',
                questionType: 5,
                order: 2,
                answers: [],
            },
        ],
    },
    revision: 0,
    startedAt: '2026-09-12T08:00:00Z',
    expiresAt: '2026-09-12T08:20:00Z',
    serverTime: '2026-09-12T08:00:00Z',
};

describe('QuizAttemptStore', () => {
    function createStore(overrides: Partial<AttemptApi> = {}) {
        const api: AttemptApi = {
            start: vi.fn().mockResolvedValue(start),
            progress: vi.fn(),
            save: vi.fn().mockImplementation((_id, revision, answers) =>
                Promise.resolve({
                    ...start,
                    revision: revision + 1,
                    pausedAt: null,
                    lastSavedAt: '2026-09-12T08:01:00Z',
                    remainingSeconds: 1140,
                    answers,
                } satisfies AttemptProgress),
            ),
            pause: vi.fn(),
            resume: vi.fn(),
            submit: vi.fn(),
            result: vi.fn(),
            unfinished: vi.fn().mockResolvedValue([]),
            history: vi.fn(),
            historyPage: vi.fn(),
            ...overrides,
        };
        TestBed.configureTestingModule({
            providers: [
                QuizAttemptStore,
                {provide: ATTEMPT_API, useValue: api},
                {provide: ATTEMPT_CONFIG, useValue: {saveDelayMs: 1000, tickMs: 1000}},
            ],
        });
        return {store: TestBed.inject(QuizAttemptStore), api};
    }

    it('starts a server attempt and maps its questions', async () => {
        const {store, api} = createStore();

        await store.load(quizId);

        expect(api.unfinished).toHaveBeenCalledWith(quizId);
        expect(api.start).toHaveBeenCalledWith(quizId);
        expect(store.title()).toBe('C# basics');
        expect(store.questions()).toHaveLength(2);
        expect(store.formattedTime()).toBe('20:00');
    });

    it('saves edited answers with the current revision', async () => {
        const {store, api} = createStore();
        await store.load(quizId);

        store.toggleOption('30000000-0000-0000-0000-000000000001');
        expect(await store.save()).toBe(true);

        expect(api.save).toHaveBeenCalledWith(attemptId, 0, [
            {
                questionId: '20000000-0000-0000-0000-000000000001',
                answerIds: ['30000000-0000-0000-0000-000000000001'],
            },
        ]);
        expect(store.dirty()).toBe(false);
    });
});
