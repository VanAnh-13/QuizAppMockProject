import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { AuthSession } from '../../../core/auth/auth-session';
import { AuthDialogComponent } from '../../../shared/ui/auth-dialog/auth-dialog.component';
import { ATTEMPT_API } from '../../quiz-attempt/application/attempt-api';
import { QUIZ_DETAILS_CONTENT } from '../infrastructure/quiz-details-content.provider';
import { QuizDetailsPage } from './quiz-details.page';

const quizId = '10000000-0000-0000-0000-000000000003';

describe('QuizDetailsPage', () => {
  const load = vi.fn();
  const history = vi.fn();

  beforeEach(async () => {
    sessionStorage.clear();
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

  afterEach(() => {
    sessionStorage.clear();
    vi.restoreAllMocks();
  });

  it('shows working login and registration controls for anonymous visitors', async () => {
    const open = vi.spyOn(AuthDialogComponent.prototype, 'open').mockImplementation(() => {});
    const fixture = await createFixture();
    const header = (fixture.nativeElement as HTMLElement).querySelector('header')!;

    expect(header.querySelector('.details-header__profile')).toBeNull();
    expect(header.textContent).not.toContain('Anh LV');
    header.querySelector<HTMLButtonElement>('[data-testid="header-login"]')!.click();
    expect(open).toHaveBeenLastCalledWith();
    header.querySelector<HTMLButtonElement>('[data-testid="header-register"]')!.click();
    expect(open).toHaveBeenLastCalledWith(true);
    expect(history).not.toHaveBeenCalled();
  });

  it('updates the header on login and returns to signed-out controls on logout', async () => {
    const fixture = await createFixture();
    const session = TestBed.inject(AuthSession);
    const header = (fixture.nativeElement as HTMLElement).querySelector('header')!;

    session.set({
      token: 'test-token',
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
      userDto: { id: 'learner-id', username: 'linh', fullName: 'Nguyễn Linh' },
    });
    fixture.detectChanges();

    expect(header.querySelector('.details-header__profile')?.textContent).toContain('Nguyễn Linh');
    expect(header.querySelector('[data-testid="header-login"]')).toBeNull();
    expect(header.querySelector('[data-testid="header-register"]')).toBeNull();

    header.querySelector<HTMLButtonElement>('[data-testid="header-logout"]')!.click();
    fixture.detectChanges();

    expect(session.user()).toBeNull();
    expect(session.token()).toBeNull();
    expect(header.querySelector('.details-header__profile')).toBeNull();
    expect(header.querySelector('[data-testid="header-login"]')).not.toBeNull();
  });

  it('falls back to the username and reacts when the session is cleared elsewhere', async () => {
    const session = TestBed.inject(AuthSession);
    session.set({
      token: 'test-token',
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
      userDto: { id: 'learner-id', username: 'learner', fullName: null },
    });
    const fixture = await createFixture();
    const header = (fixture.nativeElement as HTMLElement).querySelector('header')!;

    expect(header.querySelector('.details-header__profile')?.textContent).toContain('learner');
    session.clear();
    fixture.detectChanges();

    expect(header.querySelector('.details-header__profile')).toBeNull();
    expect(header.querySelector('[data-testid="header-login"]')).not.toBeNull();
  });

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
