import {TestBed} from '@angular/core/testing';
import {ApiClient} from '../../../core/api/api-client';
import {ApiAdminQuiz} from './api-admin-quiz';

describe('ApiAdminQuiz', () => {
    it('maps a paged quiz list and creates a quiz', async () => {
        const payload = {
            id: 'quiz-1',
            title: 'C# cơ bản',
            description: null,
            duration: 20,
            image: null,
            passedScore: 70,
            isActive: true,
        };
        const get = vi.fn().mockResolvedValue({
            items: [payload],
            totalCount: 1,
            pageNumber: 1,
            pageSize: 6,
        });
        const post = vi.fn().mockResolvedValue(payload);
        TestBed.configureTestingModule({
            providers: [ApiAdminQuiz, {provide: ApiClient, useValue: {get, post}}],
        });
        const api = TestBed.inject(ApiAdminQuiz);

        const page = await api.list(1, 6, '');
        await api.create({
            title: payload.title,
            description: null,
            duration: 20,
            image: null,
            passedScore: 70,
            isActive: true,
        });

        expect(page.items[0].title).toBe('C# cơ bản');
        expect(post).toHaveBeenCalledWith('quizzes', expect.objectContaining({title: 'C# cơ bản'}));
    });
});
