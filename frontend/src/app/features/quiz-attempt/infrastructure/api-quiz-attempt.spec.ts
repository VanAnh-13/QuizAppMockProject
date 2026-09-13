import { TestBed } from '@angular/core/testing';
import { ApiClient } from '../../../core/api/api-client';
import { ApiQuizAttempt } from './api-quiz-attempt';

describe('ApiQuizAttempt', () => {
  it('uses the protected attempt endpoints and revision payloads', async () => {
    const api = {
      get: vi.fn().mockResolvedValue({}),
      post: vi.fn().mockResolvedValue({}),
      put: vi.fn().mockResolvedValue({}),
      list: vi.fn().mockResolvedValue([]),
    };
    TestBed.configureTestingModule({
      providers: [ApiQuizAttempt, { provide: ApiClient, useValue: api }],
    });
    const attempts = TestBed.inject(ApiQuizAttempt);
    const answer = { questionId: 'question-id', answerIds: ['answer-id'] };

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
    expect(api.post).toHaveBeenCalledWith('attempts/attempt-id/resume', { revision: 4 });
    expect(api.post).toHaveBeenCalledWith('attempts/attempt-id/submit', { revision: 5 });
    expect(api.list).toHaveBeenCalledWith('quiz-history', { quizId: 'quiz-id' });
  });
});
