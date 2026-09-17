import {TestBed} from '@angular/core/testing';
import {ActivatedRouteSnapshot, RouterStateSnapshot} from '@angular/router';
import {QuizDetailsSnapshot} from '../application/quiz-details-content';
import {QUIZ_DETAILS_CONTENT} from '../infrastructure/quiz-details-content.provider';
import {quizDetailsResolver} from './quiz-details.resolver';

describe('quizDetailsResolver', () => {
    const mockContent = {
        load: vi.fn(),
    };

    const mockSnapshot: QuizDetailsSnapshot = {
        title: 'C# fundamentals',
        description: 'Test description',
        categoryLabel: 'Programming',
        metrics: [],
        topics: [],
        guidelines: [],
        formatFacts: [],
    };

    beforeEach(() => {
        mockContent.load.mockReset();
        TestBed.configureTestingModule({
            providers: [{provide: QUIZ_DETAILS_CONTENT, useValue: mockContent}],
        });
    });

    it('returns quiz details snapshot when load succeeds', async () => {
        mockContent.load.mockResolvedValue(mockSnapshot);
        const route = {
            paramMap: {
                get: (key: string) => (key === 'quizId' ? 'quiz-123' : null),
            },
        } as unknown as ActivatedRouteSnapshot;

        const result = await TestBed.runInInjectionContext(() =>
            quizDetailsResolver(route, {} as RouterStateSnapshot),
        );

        expect(result).toEqual(mockSnapshot);
        expect(mockContent.load).toHaveBeenCalledWith('quiz-123');
    });

    it('returns null when quizId is missing', async () => {
        const route = {
            paramMap: {
                get: () => null,
            },
        } as unknown as ActivatedRouteSnapshot;

        const result = await TestBed.runInInjectionContext(() =>
            quizDetailsResolver(route, {} as RouterStateSnapshot),
        );

        expect(result).toBeNull();
        expect(mockContent.load).not.toHaveBeenCalled();
    });

    it('returns null when load rejects', async () => {
        mockContent.load.mockRejectedValue(new Error('Network error'));
        const route = {
            paramMap: {
                get: (key: string) => (key === 'quizId' ? 'quiz-123' : null),
            },
        } as unknown as ActivatedRouteSnapshot;

        const result = await TestBed.runInInjectionContext(() =>
            quizDetailsResolver(route, {} as RouterStateSnapshot),
        );

        expect(result).toBeNull();
    });
});
