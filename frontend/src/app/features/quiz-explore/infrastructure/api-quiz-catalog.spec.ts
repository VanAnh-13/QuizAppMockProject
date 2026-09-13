import { TestBed } from '@angular/core/testing';
import { ApiClient } from '../../../core/api/api-client';
import { ApiQuizCatalog } from './api-quiz-catalog';

describe('ApiQuizCatalog', () => {
  it('loads and maps the public quiz catalog contract', async () => {
    const list = vi.fn().mockResolvedValue([
      {
        id: '10000000-0000-0000-0000-000000000001',
        title: 'C# basics',
        description: null,
        duration: 20,
        passedScore: 70,
        questionCount: 3,
        updatedAt: '2026-09-12T00:00:00Z',
      },
    ]);
    TestBed.configureTestingModule({
      providers: [ApiQuizCatalog, { provide: ApiClient, useValue: { list } }],
    });

    const quizzes = await TestBed.inject(ApiQuizCatalog).listQuizzes();

    expect(list).toHaveBeenCalledWith('public/quizzes');
    expect(quizzes).toEqual([
      expect.objectContaining({
        id: '10000000-0000-0000-0000-000000000001',
        title: 'C# basics',
        description: '',
        durationMinutes: 20,
        questionCount: 3,
      }),
    ]);
  });
});
