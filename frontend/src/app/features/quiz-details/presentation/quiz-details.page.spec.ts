import {TestBed} from '@angular/core/testing';
import {HttpErrorResponse} from '@angular/common/http';
import {ActivatedRoute, convertToParamMap, provideRouter, Router} from '@angular/router';
import {of} from 'rxjs';
import {AuthSession} from '../../../core/auth/auth-session';
import {ATTEMPT_API} from '../../quiz-attempt/application/attempt-api';
import {QUIZ_DETAILS_CONTENT} from '../infrastructure/quiz-details-content.provider';
import {ConfirmationService} from '../../../shared/ui/confirmation/confirmation.service';
import {MockConfirmationService} from '../../../../testing/mock-confirmation';
import {QuizDetailsPage} from './quiz-details.page';

const quizId = '10000000-0000-0000-0000-000000000003';

describe('QuizDetailsPage', () => {
    const load = vi.fn();
    const history = vi.fn();

    const originalShowModal = Object.getOwnPropertyDescriptor(HTMLDialogElement.prototype, 'showModal');
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
            }),
        });
        sessionStorage.clear();
        load.mockReset();
        history.mockReset();
        load.mockResolvedValue({
            title: 'C# fundamentals',
            description: 'Quiz description',
            categoryLabel: 'Quiz',
            metrics: [{icon: 'quiz', label: 'Số câu hỏi', value: '3 câu hỏi'}],
            topics: [],
            guidelines: [],
            formatFacts: [{label: 'Trạng thái', value: 'Đang mở'}],
        });
        history.mockResolvedValue([
            {
                id: '40000000-0000-0000-0000-000000000001',
                quizId,
                quizTitle: 'C# fundamentals',
                submittedAt: '2026-09-12T08:00:00Z',
                score: 80,
                passedScore: 70,
            },
        ]);
        await TestBed.configureTestingModule({
            imports: [QuizDetailsPage],
            providers: [
                provideRouter([]),
                {provide: ActivatedRoute, useValue: {paramMap: of(convertToParamMap({quizId}))}},
                {provide: ConfirmationService, useClass: MockConfirmationService},
            ],
        })
            .overrideComponent(QuizDetailsPage, {
                set: {
                    providers: [
                        {provide: QUIZ_DETAILS_CONTENT, useValue: {load}},
                        {provide: ATTEMPT_API, useValue: {history}},
                    ],
                },
            })
            .compileComponents();
        vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    });

    async function createFixture() {
        const fixture = TestBed.createComponent(QuizDetailsPage);
        fixture.detectChanges();
        await fixture.whenStable();
        fixture.detectChanges();
        return fixture;
    }

    afterEach(() => {
        TestBed.resetTestingModule();
        sessionStorage.clear();
        vi.restoreAllMocks();
        if (originalShowModal) Object.defineProperty(HTMLDialogElement.prototype, 'showModal', originalShowModal);
        else Reflect.deleteProperty(HTMLDialogElement.prototype, 'showModal');
        if (originalClose) Object.defineProperty(HTMLDialogElement.prototype, 'close', originalClose);
        else Reflect.deleteProperty(HTMLDialogElement.prototype, 'close');
    });

    it('shows working login and registration controls for anonymous visitors', async () => {
        const fixture = await createFixture();
        const header = (fixture.nativeElement as HTMLElement).querySelector('header')!;

        expect(header.querySelector('.details-header__profile')).toBeNull();
        expect(header.textContent).not.toContain('Anh LV');
        expect(header.querySelector('.details-header__user-link')).toBeNull();
        expect(header.querySelector('[data-testid="header-login"]')?.getAttribute('href')).toBe(
            `/login?returnUrl=${encodeURIComponent(`/quiz/${quizId}`)}`,
        );
        expect(header.querySelector('[data-testid="header-register"]')?.getAttribute('href')).toBe(
            `/register?returnUrl=${encodeURIComponent(`/quiz/${quizId}`)}`,
        );
        expect((fixture.nativeElement as HTMLElement).querySelector('app-auth-dialog')).toBeNull();
        expect(history).not.toHaveBeenCalled();
    });

    it.each(['mobile', 'desktop'])('sends anonymous %s history visitors to login without requesting protected history', async (layout) => {
        const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
        const fixture = await createFixture();
        const element = fixture.nativeElement as HTMLElement;

        element.querySelector<HTMLButtonElement>(`.quiz-details__actions--${layout} .history-button`)!.click();
        await fixture.whenStable();

        expect(navigate).toHaveBeenCalledWith(['/login'], {queryParams: {returnUrl: `/quiz/${quizId}`}});
        expect(history).not.toHaveBeenCalled();
        expect(element.querySelector('#attempt-history')).toBeNull();
    });

    it('updates the header on login and returns to signed-out controls on logout', async () => {
        const fixture = await createFixture();
        const session = TestBed.inject(AuthSession);
        const header = (fixture.nativeElement as HTMLElement).querySelector('header')!;

        session.set({
            token: 'test-token',
            expiresAt: new Date(Date.now() + 60_000).toISOString(),
            userDto: {id: 'learner-id', username: 'linh', fullName: 'Nguyễn Linh'},
        });
        fixture.detectChanges();

        expect(header.querySelector('.details-header__profile')?.textContent).toContain('Nguyễn Linh');
        const accountLinks = [...header.querySelectorAll<HTMLAnchorElement>('.details-header__user-link')];
        expect(accountLinks.map((link) => link.getAttribute('href'))).toEqual(['/history', '/settings']);
        expect(accountLinks[0]?.textContent).toContain('Attempt history');
        expect(accountLinks[1]?.textContent).toContain('Settings & security');
        for (const link of accountLinks) {
            link.click();
            expect(TestBed.inject(Router).navigateByUrl).toHaveBeenLastCalledWith(
                TestBed.inject(Router).parseUrl(link.getAttribute('href')!),
                expect.objectContaining({skipLocationChange: false}),
            );
        }
        expect(header.querySelector('[data-testid="header-login"]')).toBeNull();
        expect(header.querySelector('[data-testid="header-register"]')).toBeNull();

        const navigateByUrl = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
        header.querySelector<HTMLButtonElement>('[data-testid="header-logout"]')!.click();
        await fixture.whenStable();
        fixture.detectChanges();

        expect(session.user()).toBeNull();
        expect(session.token()).toBeNull();
        expect(header.querySelector('.details-header__profile')).toBeNull();
        expect(header.querySelector('[data-testid="header-login"]')).not.toBeNull();
        expect(navigateByUrl).toHaveBeenCalledWith('/login');
    });

    it('does not log out when confirmation is cancelled on quiz details page', async () => {
        vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
        const fixture = await createFixture();
        const session = TestBed.inject(AuthSession);
        const confirmation = TestBed.inject(ConfirmationService) as unknown as MockConfirmationService;
        confirmation.setAutoResponse(false);
        const header = (fixture.nativeElement as HTMLElement).querySelector('header')!;

        session.set({
            token: 'test-token',
            expiresAt: new Date(Date.now() + 60_000).toISOString(),
            userDto: {id: 'learner-id', username: 'linh', fullName: 'Nguyễn Linh'},
        });
        fixture.detectChanges();

        header.querySelector<HTMLButtonElement>('[data-testid="header-logout"]')!.click();
        await fixture.whenStable();
        fixture.detectChanges();

        expect(session.user()).not.toBeNull();
        expect(header.querySelector('.details-header__profile')).not.toBeNull();
    });

    it('falls back to the username and reacts when the session is cleared elsewhere', async () => {
        const session = TestBed.inject(AuthSession);
        session.set({
            token: 'test-token',
            expiresAt: new Date(Date.now() + 60_000).toISOString(),
            userDto: {id: 'learner-id', username: 'learner', fullName: null},
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

    it('explains missing topics without showing an empty heading or grid', async () => {
        const fixture = await createFixture();
        const element = fixture.nativeElement as HTMLElement;

        expect(element.querySelector('#topics-title')).toBeNull();
        expect(element.querySelector('.topics-grid')).toBeNull();
        expect(element.textContent).toContain('Topic details are not available for this quiz yet.');
        expect(element.querySelector('.start-button')).not.toBeNull();
    });

    it('shows topic cards when topic information is available', async () => {
        load.mockResolvedValue({
            title: 'C# fundamentals',
            description: 'Quiz description',
            categoryLabel: 'Quiz',
            metrics: [],
            topics: [{title: 'Kiểu dữ liệu', description: 'Các kiểu dữ liệu cơ bản trong C#.'}],
            guidelines: [],
            formatFacts: [],
        });
        const fixture = await createFixture();
        const element = fixture.nativeElement as HTMLElement;

        expect(element.querySelector('#topics-title')?.textContent).toContain(
            'What you will be tested on:',
        );
        expect(element.querySelectorAll('.topics-grid .topic-card')).toHaveLength(1);
        expect(element.querySelector('.topic-card h3')?.textContent).toContain('Kiểu dữ liệu');
        expect(element.querySelector('.topic-card p')?.textContent).toContain(
            'Các kiểu dữ liệu cơ bản trong C#.',
        );
        expect(element.textContent).not.toContain('Topic details are not available for this quiz yet.');
    });

    it.each(['mobile', 'desktop'])('loads signed-in history from the %s actions', async (layout) => {
        TestBed.inject(AuthSession).set({
            token: 'test-token',
            expiresAt: new Date(Date.now() + 60_000).toISOString(),
            userDto: {id: 'learner-id', username: 'learner', fullName: null},
        });
        const fixture = await createFixture();
        const element = fixture.nativeElement as HTMLElement;

        element.querySelector<HTMLButtonElement>(`.quiz-details__actions--${layout} .history-button`)!.click();
        await fixture.whenStable();
        fixture.detectChanges();

        expect(history).toHaveBeenCalledWith(quizId);
        expect(element.querySelector('.history-list')?.textContent).toContain('80 / 100');
    });

    it('gives responsive actions unique accessible headings and the same quiz destination', async () => {
        const fixture = await createFixture();
        const element = fixture.nativeElement as HTMLElement;
        const actions = [...element.querySelectorAll<HTMLElement>('.cta-card')];
        const headingIds = actions.map((action) => action.getAttribute('aria-labelledby'));

        expect(actions).toHaveLength(2);
        expect(new Set(headingIds).size).toBe(actions.length);
        for (const action of actions) {
            const headingId = action.getAttribute('aria-labelledby');
            expect(action.querySelector('h2')?.id).toBe(headingId);
            expect(action.querySelector('h2')?.textContent).toContain('Ready to test your skills?');
            expect(action.querySelector('a')?.getAttribute('href')).toBe(`/quiz/${quizId}/attempt`);
        }
    });

    it('links an expired history session to the full login page', async () => {
        TestBed.inject(AuthSession).set({
            token: 'test-token',
            expiresAt: new Date(Date.now() + 60_000).toISOString(),
            userDto: {id: 'learner-id', username: 'learner', fullName: null},
        });
        history.mockRejectedValue(new HttpErrorResponse({status: 401}));
        const fixture = await createFixture();
        const element = fixture.nativeElement as HTMLElement;

        element.querySelector<HTMLButtonElement>('.history-button')!.click();
        await fixture.whenStable();
        fixture.detectChanges();

        expect(element.querySelector('#attempt-history a')?.getAttribute('href')).toBe(
            `/login?returnUrl=${encodeURIComponent(`/quiz/${quizId}`)}`,
        );
    });
    it('renders the sidebar after the content column in DOM order for correct mobile stacking', async () => {
        const fixture = await createFixture();
        const element = fixture.nativeElement as HTMLElement;
        const content = element.querySelector('.quiz-details__content')!;
        const sidebar = element.querySelector('.quiz-details__sidebar')!;

        expect(content).not.toBeNull();
        expect(sidebar).not.toBeNull();
        // sidebar must follow content in DOM so that mobile (single-column) order is:
        // overview → CTA → topics → guidelines → format
        const position = content.compareDocumentPosition(sidebar);
        expect(position & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
    });

    it('places guidelines-card after the CTA wrapper in sidebar DOM order', async () => {
        const fixture = await createFixture();
        const element = fixture.nativeElement as HTMLElement;
        const sidebar = element.querySelector('.quiz-details__sidebar')!;
        const ctaWrapper = sidebar.querySelector('.quiz-details__actions--desktop')!;
        const guidelines = sidebar.querySelector('.guidelines-card')!;
        const format = sidebar.querySelector('.format-card')!;

        expect(ctaWrapper).not.toBeNull();
        expect(guidelines).not.toBeNull();
        expect(format).not.toBeNull();
        // guidelines must follow cta wrapper
        expect(ctaWrapper.compareDocumentPosition(guidelines) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
        // format must follow guidelines
        expect(guidelines.compareDocumentPosition(format) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
    });
    it('opens information as a modal and dismisses it on Escape', async () => {
        const fixture = await createFixture();
        const element = fixture.nativeElement as HTMLElement;
        element.querySelector<HTMLButtonElement>('[title="Notifications"]')!.click();
        fixture.detectChanges();
        const dialog = element.querySelector<HTMLDialogElement>('.details-dialog')!;
        expect(HTMLDialogElement.prototype.showModal).toHaveBeenCalled();
        expect(dialog.open).toBe(true);
        dialog.dispatchEvent(new Event('cancel'));
        fixture.detectChanges();
        expect(element.querySelector('.details-dialog')).toBeNull();
    });
});
