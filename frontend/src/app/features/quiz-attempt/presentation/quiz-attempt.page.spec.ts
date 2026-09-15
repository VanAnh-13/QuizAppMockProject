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
    });
    fixture.detectChanges();

    expect(element.querySelector<HTMLDialogElement>('#submit-confirmation')?.open).toBe(false);
  });
});
