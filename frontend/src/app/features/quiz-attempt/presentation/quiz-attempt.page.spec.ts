import {TestBed} from '@angular/core/testing';
import {ActivatedRoute, convertToParamMap, provideRouter} from '@angular/router';
import {of} from 'rxjs';
import {ATTEMPT_API, AttemptApi, AttemptStart} from '../application/attempt-api';
import {QuizAttemptStore} from './quiz-attempt.store';
import {QuizAttemptPage} from './quiz-attempt.page';

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
                answers: [{id: '30000000-0000-0000-0000-000000000001', text: 'Answer'}],
            },
        ],
    },
    revision: 0,
    startedAt: '2026-09-12T08:00:00Z',
    expiresAt: '2026-09-12T08:20:00Z',
    serverTime: '2026-09-12T08:00:00Z',
};

describe('QuizAttemptPage', () => {
    beforeEach(async () => {
        const api: Partial<AttemptApi> = {
            unfinished: vi.fn().mockResolvedValue([]),
            start: vi.fn().mockResolvedValue(start),
        };
        const paramMap = convertToParamMap({quizId});
        await TestBed.configureTestingModule({
            imports: [QuizAttemptPage],
            providers: [
                provideRouter([]),
                {
                    provide: ActivatedRoute,
                    useValue: {
                        paramMap: of(paramMap),
                        snapshot: {paramMap, queryParamMap: convertToParamMap({})},
                    },
                },
            ],
        })
            .overrideComponent(QuizAttemptPage, {
                set: {
                    providers: [QuizAttemptStore, {provide: ATTEMPT_API, useValue: api}],
                },
            })
            .compileComponents();
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

    it('opens submission confirmation for the loaded attempt', async () => {
        const fixture = await createFixture();
        const element = fixture.nativeElement as HTMLElement;

        element.querySelector<HTMLButtonElement>('.submit-button')!.click();
        fixture.detectChanges();

        expect(element.querySelector<HTMLDialogElement>('#submit-confirmation')?.open).toBe(true);
    });
});
