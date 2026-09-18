import {TestBed} from '@angular/core/testing';
import {ApiClient} from '../../../core/api/api-client';
import {ApiQuizDetailsContent} from './api-quiz-details-content';

describe('ApiQuizDetailsContent', () => {
    it('selects the route quiz from the public catalog', async () => {
        const list = vi.fn().mockResolvedValue([
            {
                id: '10000000-0000-0000-0000-000000000002',
                title: 'Angular forms',
                description: 'Reactive forms practice',
                duration: 25,
                passedScore: 75,
                questionCount: 4,
                updatedAt: '2026-09-12T00:00:00Z',
            },
        ]);
        TestBed.configureTestingModule({
            providers: [ApiQuizDetailsContent, {provide: ApiClient, useValue: {list}}],
        });

        const details = await TestBed.inject(ApiQuizDetailsContent).load(
            '10000000-0000-0000-0000-000000000002',
        );

        expect(list).toHaveBeenCalledWith('public/quizzes');
        expect(details.title).toBe('Angular forms');
        expect(details.metrics.map((metric) => metric.value)).toEqual(['4 questions', '25 minutes', '75%']);
    });

    it('rejects a quiz that is absent from the active catalog', async () => {
        TestBed.configureTestingModule({
            providers: [
                ApiQuizDetailsContent,
                {provide: ApiClient, useValue: {list: vi.fn().mockResolvedValue([])}},
            ],
        });

        await expect(TestBed.inject(ApiQuizDetailsContent).load('missing')).rejects.toThrow(
            'This quiz is not available.',
        );
    });

    it('preserves configured imageUrl from the public quiz contract in details', async () => {
        const list = vi.fn().mockResolvedValue([
            {
                id: '10000000-0000-0000-0000-000000000002',
                title: 'Angular forms',
                description: 'Reactive forms practice',
                duration: 25,
                imageUrl: 'https://images.example.com/custom-angular.webp',
                passedScore: 75,
                questionCount: 4,
                updatedAt: '2026-09-12T00:00:00Z',
            },
        ]);
        TestBed.configureTestingModule({
            providers: [ApiQuizDetailsContent, {provide: ApiClient, useValue: {list}}],
        });

        const details = await TestBed.inject(ApiQuizDetailsContent).load(
            '10000000-0000-0000-0000-000000000002',
        );

        expect(details.imageUrl).toBe('https://images.example.com/custom-angular.webp');
    });
});
