import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { ATTEMPT_API, AttemptApi, AttemptStart } from '../application/attempt-api';
import { QuizAttemptStore } from './quiz-attempt.store';
import { QuizAttemptPage } from './quiz-attempt.page';

const quizId = '10000000-0000-0000-0000-000000000001';
const start: AttemptStart = {
  attemptId: '40000000-0000-0000-0000-000000000001',
  quiz: {
    quizId,
    title: 'C# basics',
    questions: [
      {
        id: '20000000-0000-0000-0000-000000000001',
        content: 'Choose one answer',
        questionType: 3,
        order: 1,
        answers: [{ id: '30000000-0000-0000-0000-000000000001', text: 'Answer' }],
      },
    ],
  },
  revision: 0,
  startedAt: '2026-09-12T08:00:00Z',
  expiresAt: '2026-09-12T08:20:00Z',
  serverTime: '2026-09-12T08:00:00Z',
};

describe('QuizAttemptPage', () => {
  const originalShowModal = Object.getOwnPropertyDescriptor(
    HTMLDialogElement.prototype,
    'showModal',
  );
  const originalClose = Object.getOwnPropertyDescriptor(HTMLDialogElement.prototype, 'close');

  beforeEach(async () => {
    Object.defineProperty(HTMLDialogElement.prototype, 'showModal', {
      configurable: true,
      value: vi.fn(function (this: HTMLDialogElement) {
        this.setAttribute('open', '');
      }),
    });
    Object.defineProperty(HTMLDialogElement.prototype, 'close', {
      configurable: true,
      value: vi.fn(function (this: HTMLDialogElement) {
        this.removeAttribute('open');
        this.dispatchEvent(new Event('close'));
      }),
    });
    const api: Partial<AttemptApi> = {
      unfinished: vi.fn().mockResolvedValue([]),
      start: vi.fn().mockResolvedValue(start),
    };
    const paramMap = convertToParamMap({ quizId });
    await TestBed.configureTestingModule({
      imports: [QuizAttemptPage],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            paramMap: of(paramMap),
            snapshot: { paramMap, queryParamMap: convertToParamMap({}) },
          },
        },
      ],
    })
      .overrideComponent(QuizAttemptPage, {
        set: {
          providers: [QuizAttemptStore, { provide: ATTEMPT_API, useValue: api }],
        },
      })
      .compileComponents();
  });

  afterEach(() => {
    vi.restoreAllMocks();
    if (originalShowModal) {
      Object.defineProperty(HTMLDialogElement.prototype, 'showModal', originalShowModal);
    } else {
      Reflect.deleteProperty(HTMLDialogElement.prototype, 'showModal');
    }
    if (originalClose) {
      Object.defineProperty(HTMLDialogElement.prototype, 'close', originalClose);
    } else {
      Reflect.deleteProperty(HTMLDialogElement.prototype, 'close');
    }
  });

  async function createFixture() {
    const fixture = TestBed.createComponent(QuizAttemptPage);
    fixture.detectChanges();
    await (fixture.componentInstance as unknown as { load(): Promise<void> }).load();
    fixture.detectChanges();
    return fixture;
  }

  it('renders the server attempt', async () => {
    const fixture = await createFixture();
    const element = fixture.nativeElement as HTMLElement;

    expect(element.querySelector('h1')?.textContent).toContain('C# basics');
    expect(element.querySelector('.question-card h2')?.textContent).toContain('Choose one answer');
    expect(element.querySelectorAll('.question-palette button')).toHaveLength(1);
  });

  it('keeps the current attempt in the login destination after an authentication error', async () => {
    const fixture = await createFixture();
    const store = fixture.debugElement.injector.get(QuizAttemptStore);
    store.errorMessage.set('Vui lòng đăng nhập lại để tiếp tục.');
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;

    expect(element.querySelector('[role="alert"] a')?.getAttribute('href')).toBe(
      `/login?returnUrl=${encodeURIComponent(`/quiz/${quizId}/attempt?attemptId=${start.attemptId}`)}`,
    );
    expect(element.querySelector('app-auth-dialog')).toBeNull();
  });

  it('opens submission confirmation for the loaded attempt', async () => {
    const fixture = await createFixture();
    const element = fixture.nativeElement as HTMLElement;

    element.querySelector<HTMLButtonElement>('.submit-button')!.click();
    fixture.detectChanges();

    expect(element.querySelector<HTMLDialogElement>('#submit-confirmation')?.open).toBe(true);
    expect(HTMLDialogElement.prototype.showModal).toHaveBeenCalledOnce();
  });

  it('cancels confirmation and restores attempt keyboard navigation', async () => {
    const fixture = await createFixture();
    const element = fixture.nativeElement as HTMLElement;
    const store = fixture.debugElement.injector.get(QuizAttemptStore);
    const next = vi.spyOn(store, 'nextQuestion');
    element.querySelector<HTMLButtonElement>('.submit-button')!.click();
    fixture.detectChanges();
    const dialog = element.querySelector<HTMLDialogElement>('#submit-confirmation')!;

    window.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowRight' }));
    expect(next).not.toHaveBeenCalled();
    dialog.dispatchEvent(new Event('cancel', { cancelable: true }));
    fixture.detectChanges();

    expect(dialog.open).toBe(false);
    window.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowRight' }));
    expect(next).toHaveBeenCalledOnce();
    element.querySelector<HTMLButtonElement>('.submit-button')!.click();
    expect(dialog.open).toBe(true);
  });

  it('keeps confirmation modal while submission is busy', async () => {
    const fixture = await createFixture();
    const element = fixture.nativeElement as HTMLElement;
    const store = fixture.debugElement.injector.get(QuizAttemptStore);
    element.querySelector<HTMLButtonElement>('.submit-button')!.click();
    store.isBusy.set(true);
    fixture.detectChanges();
    const dialog = element.querySelector<HTMLDialogElement>('#submit-confirmation')!;
    const cancel = new Event('cancel', { cancelable: true });

    dialog.dispatchEvent(cancel);

    expect(cancel.defaultPrevented).toBe(true);
    expect(dialog.open).toBe(true);
    expect(dialog.querySelector<HTMLButtonElement>('button[autofocus]')?.disabled).toBe(true);
  });

  it('closes confirmation when the attempt receives a result', async () => {
    const fixture = await createFixture();
    const element = fixture.nativeElement as HTMLElement;
    const store = fixture.debugElement.injector.get(QuizAttemptStore);
    element.querySelector<HTMLButtonElement>('.submit-button')!.click();
    store.result.set({
      id: start.attemptId,
      quizId,
      quizTitle: start.quiz.title,
      score: 100,
      submittedAt: '2026-09-12T08:10:00Z',
      passedScore: 70,
    });
    fixture.detectChanges();

    expect(element.querySelector<HTMLDialogElement>('#submit-confirmation')?.open).toBe(false);
  });

  it('marks a score at or above the quiz threshold as passed', async () => {
    const fixture = await createFixture();
    const element = fixture.nativeElement as HTMLElement;
    const store = fixture.debugElement.injector.get(QuizAttemptStore);
    store.result.set({
      id: start.attemptId,
      quizId,
      quizTitle: start.quiz.title,
      score: 75,
      submittedAt: '2026-09-12T08:10:00Z',
      passedScore: 70,
    });
    fixture.detectChanges();

    const pill = element.querySelector('.score-status-pill')!;
    expect(pill.textContent).toContain('ĐẠT TIÊU CHUẨN');
    expect(pill.classList.contains('score-status-pill--pass')).toBe(true);
    expect(element.textContent).not.toContain('CHƯA ĐẠT CHUẨN');
    expect(element.textContent).toContain('70%');

    const stops = element.querySelectorAll('#scoreGaugeGradient stop');
    expect(stops).toHaveLength(2);
    expect(stops[0].getAttribute('stop-color')).toBe('#10b981');
    expect(stops[1].getAttribute('stop-color')).toBe('#059669');
  });

  it('labels a shortfall against the quiz threshold and shows the required score', async () => {
    const fixture = await createFixture();
    const element = fixture.nativeElement as HTMLElement;
    const store = fixture.debugElement.injector.get(QuizAttemptStore);
    store.result.set({
      id: start.attemptId,
      quizId,
      quizTitle: start.quiz.title,
      score: 70,
      submittedAt: '2026-09-12T08:10:00Z',
      passedScore: 75,
    });
    fixture.detectChanges();

    const pill = element.querySelector('.score-status-pill')!;
    expect(pill.textContent).toContain('CHƯA ĐẠT CHUẨN (CẦN ≥75%)');
    expect(pill.classList.contains('score-status-pill--pass')).toBe(false);
  });

  it('renders a neutral result without a pass verdict when no threshold is configured', async () => {
    const fixture = await createFixture();
    const element = fixture.nativeElement as HTMLElement;
    const store = fixture.debugElement.injector.get(QuizAttemptStore);
    store.result.set({
      id: start.attemptId,
      quizId,
      quizTitle: start.quiz.title,
      score: 75,
      submittedAt: '2026-09-12T08:10:00Z',
      passedScore: null,
    });
    fixture.detectChanges();

    const pill = element.querySelector('.score-status-pill')!;
    expect(pill.textContent).toContain('KHÔNG QUY ĐỊNH ĐIỂM ĐẠT');
    expect(pill.classList.contains('score-status-pill--pass')).toBe(false);
    expect(element.querySelector('.breakdown-bar__fill--target')).toBeNull();
    expect(element.textContent).toContain('Không quy định');
  });

  it('uses the brand blue gradient for the score gauge when an attempt has not passed', async () => {
    const fixture = await createFixture();
    const element = fixture.nativeElement as HTMLElement;
    const store = fixture.debugElement.injector.get(QuizAttemptStore);
    store.result.set({
      id: start.attemptId,
      quizId,
      quizTitle: start.quiz.title,
      score: 70,
      submittedAt: '2026-09-12T08:10:00Z',
      passedScore: 75,
    });
    fixture.detectChanges();

    const stops = element.querySelectorAll('#scoreGaugeGradient stop');
    expect(stops).toHaveLength(2);
    expect(stops[0].getAttribute('stop-color')).toBe('#1d4ed8');
    expect(stops[1].getAttribute('stop-color')).toBe('#38bdf8');
    expect(element.querySelector('.score-gauge__track')?.getAttribute('stroke')).toBe('#eff6ff');
  });

  it('maintains identity, progress, and actions elements in order within exam header', async () => {
    const fixture = await createFixture();
    const element = fixture.nativeElement as HTMLElement;

    const headerInner = element.querySelector('.exam-header__inner');
    expect(headerInner).not.toBeNull();
    const childClasses = Array.from(headerInner!.children).map(c => c.className);
    expect(childClasses).toEqual(['exam-header__identity', 'exam-progress', 'exam-header__actions']);
  });

  it('replaces next button with submit button on the last question', async () => {
    const fixture = await createFixture();
    const store = fixture.debugElement.injector.get(QuizAttemptStore);
    const element = fixture.nativeElement as HTMLElement;

    const question1 = store.questions()[0];
    const question2 = { ...question1, id: '20000000-0000-0000-0000-000000000002', number: 2 };
    store.questions.set([question1, question2]);
    store.currentQuestionNumber.set(1);
    fixture.detectChanges();

    expect(element.querySelector('.next-button')).not.toBeNull();
    expect(element.querySelector('.submit-button--nav')).toBeNull();

    store.currentQuestionNumber.set(2);
    fixture.detectChanges();

    expect(element.querySelector('.next-button')).toBeNull();
    const submitNavBtn = element.querySelector<HTMLButtonElement>('.submit-button--nav');
    expect(submitNavBtn).not.toBeNull();
    expect(submitNavBtn?.textContent).toContain('Nộp bài');
  });

  it('opens liquid glass leave dialog when canLeave() is called during an active attempt and resolves false on cancel', async () => {
    const fixture = await createFixture();
    const component = fixture.componentInstance;
    const element = fixture.nativeElement as HTMLElement;

    const confirmSpy = vi.spyOn(window, 'confirm');

    const canLeavePromise = component.canLeave();
    fixture.detectChanges();

    const leaveDialog = element.querySelector<HTMLDialogElement>('#leave-confirmation');
    expect(leaveDialog).not.toBeNull();
    expect(leaveDialog?.hasAttribute('open')).toBe(true);
    expect(confirmSpy).not.toHaveBeenCalled();

    const stayButton = leaveDialog?.querySelector<HTMLButtonElement>('.btn-stay');
    expect(stayButton).not.toBeNull();
    stayButton?.click();
    fixture.detectChanges();

    const canLeaveResult = await canLeavePromise;
    expect(canLeaveResult).toBe(false);
  });

  it('resolves true when user confirms leave from the liquid glass dialog', async () => {
    const fixture = await createFixture();
    const component = fixture.componentInstance;
    const element = fixture.nativeElement as HTMLElement;

    const canLeavePromise = component.canLeave();
    fixture.detectChanges();

    const leaveDialog = element.querySelector<HTMLDialogElement>('#leave-confirmation');
    expect(leaveDialog?.hasAttribute('open')).toBe(true);

    const leaveButton = leaveDialog?.querySelector<HTMLButtonElement>('.btn-leave');
    expect(leaveButton).not.toBeNull();
    leaveButton?.click();
    fixture.detectChanges();

    const canLeaveResult = await canLeavePromise;
    expect(canLeaveResult).toBe(true);
  });

  it('allows leaving immediately without dialog when attempt is already submitted or empty', async () => {
    const fixture = await createFixture();
    const component = fixture.componentInstance;
    const store = fixture.debugElement.injector.get(QuizAttemptStore);
    const element = fixture.nativeElement as HTMLElement;

    store.result.set({
      id: start.attemptId,
      quizId,
      quizTitle: start.quiz.title,
      score: 80,
      submittedAt: '2026-09-12T08:10:00Z',
      passedScore: 75,
    });
    fixture.detectChanges();

    const canLeaveResult = await component.canLeave();
    expect(canLeaveResult).toBe(true);
    const leaveDialog = element.querySelector<HTMLDialogElement>('#leave-confirmation');
    expect(leaveDialog?.hasAttribute('open')).toBe(false);
  });
});

