import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { ATTEMPT_API } from '../../quiz-attempt/application/attempt-api';
import { QUIZ_DETAILS_CONTENT } from '../infrastructure/quiz-details-content.provider';
import { QuizDetailsPage } from './quiz-details.page';

const quizId = '10000000-0000-0000-0000-000000000003';

describe('QuizDetailsPage', () => {
  const load = vi.fn();
  const history = vi.fn();

  beforeEach(async () => {
    load.mockReset();
    history.mockReset();
    load.mockResolvedValue({
      title: 'C# fundamentals',
      description: 'Quiz description',
      categoryLabel: 'Quiz',
      metrics: [{ icon: 'quiz', label: 'Số câu hỏi', value: '3 câu hỏi' }],
      topics: [],
      guidelines: [],
      formatFacts: [{ label: 'Trạng thái', value: 'Đang mở' }],
    });
    history.mockResolvedValue([
      {
        id: '40000000-0000-0000-0000-000000000001',
        quizId,
        quizTitle: 'C# fundamentals',
        submittedAt: '2026-09-12T08:00:00Z',
        score: 80,
      },
    ]);
    await TestBed.configureTestingModule({
      imports: [QuizDetailsPage],
      providers: [
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { paramMap: of(convertToParamMap({ quizId })) } },
      ],
    })
      .overrideComponent(QuizDetailsPage, {
        set: {
          providers: [
            { provide: QUIZ_DETAILS_CONTENT, useValue: { load } },
            { provide: ATTEMPT_API, useValue: { history } },
          ],
        },
      })
      .compileComponents();
  });

  async function createFixture() {
    const fixture = TestBed.createComponent(QuizDetailsPage);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
  }

  it('loads the route quiz and links to its attempt', async () => {
    const fixture = await createFixture();
    const element = fixture.nativeElement as HTMLElement;

    expect(load).toHaveBeenCalledWith(quizId);
    expect(element.querySelector('h1')?.textContent).toContain('C# fundamentals');
    expect(element.querySelector<HTMLAnchorElement>('.start-button')?.getAttribute('href')).toBe(
      `/quiz/${quizId}/attempt`,
    );
  });

  it('loads the signed-in user history for the current quiz', async () => {
    const fixture = await createFixture();
    const element = fixture.nativeElement as HTMLElement;

    element.querySelector<HTMLButtonElement>('.history-button')!.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(history).toHaveBeenCalledWith(quizId);
    expect(element.querySelector('.history-list')?.textContent).toContain('80 / 100');
  });
});
