import {TestBed} from '@angular/core/testing';
import {ActivatedRouteSnapshot, RouterStateSnapshot} from '@angular/router';
import {QuizDetailsSnapshot} from '../application/quiz-details-content';
import {QUIZ_DETAILS_CONTENT} from '../infrastructure/quiz-details-content.provider';
import {quizDetailsResolver} from './quiz-details.resolver';
import {provideRouter} from '@angular/router';
import {RouterTestingHarness} from '@angular/router/testing';
import {QuizDetailsPage} from './quiz-details.page';
import {ATTEMPT_API} from '../../quiz-attempt/application/attempt-api';

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

    it('returns an explicit failure when quizId is missing', async () => {
        const route = {
            paramMap: {
                get: () => null,
            },
        } as unknown as ActivatedRouteSnapshot;

        const result = await TestBed.runInInjectionContext(() =>
            quizDetailsResolver(route, {} as RouterStateSnapshot),
        );

        expect(result).toEqual({errorMessage: 'Không tìm thấy mã quiz trên đường dẫn.'});
        expect(mockContent.load).not.toHaveBeenCalled();
    });

    it('returns an explicit failure when load rejects', async () => {
        mockContent.load.mockRejectedValue(new Error('Network error'));
        const route = {
            paramMap: {
                get: (key: string) => (key === 'quizId' ? 'quiz-123' : null),
            },
        } as unknown as ActivatedRouteSnapshot;

        const result = await TestBed.runInInjectionContext(() =>
            quizDetailsResolver(route, {} as RouterStateSnapshot),
        );

        expect(result).toEqual({errorMessage: 'Không thể tải chi tiết quiz từ máy chủ. Vui lòng thử lại.'});
    });

    it.each(['Không tìm thấy quiz đang mở.', 'Server unavailable'])(
        'shows a failed resolution without fetching again: %s', async (message) => {
            mockContent.load.mockRejectedValue(new Error(message));
            TestBed.configureTestingModule({
                providers: [provideRouter([
                    {path: 'quiz/:quizId', component: QuizDetailsPage, resolve: {snapshot: quizDetailsResolver}},
                ])],
            }).overrideComponent(QuizDetailsPage, {
                set: {
                    providers: [
                        {provide: QUIZ_DETAILS_CONTENT, useValue: mockContent},
                        {provide: ATTEMPT_API, useValue: {}},
                    ]
                },
            });

            const harness = await RouterTestingHarness.create();
            await harness.navigateByUrl('/quiz/missing', QuizDetailsPage);

            expect(mockContent.load).toHaveBeenCalledExactlyOnceWith('missing');
            expect(harness.routeNativeElement?.textContent).toContain('Không thể tải chi tiết quiz từ máy chủ.');

            mockContent.load.mockResolvedValue(mockSnapshot);
            const retry = Array.from(harness.routeNativeElement!.querySelectorAll('button'))
                .find((button) => button.textContent?.trim() === 'Thử lại')!;
            retry.click();
            await harness.fixture.whenStable();
            harness.detectChanges();
            expect(mockContent.load).toHaveBeenCalledTimes(2);
            expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toContain(mockSnapshot.title);

            mockContent.load.mockRejectedValue(new Error(message));
            await harness.navigateByUrl('/quiz/another-missing', QuizDetailsPage);
            expect(mockContent.load).toHaveBeenCalledTimes(3);
            expect(mockContent.load).toHaveBeenLastCalledWith('another-missing');
            expect(harness.routeNativeElement?.textContent).toContain('Không thể tải chi tiết quiz từ máy chủ.');
        },
    );
});
