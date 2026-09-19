import {TestBed} from '@angular/core/testing';
import {provideRouter} from '@angular/router';
import {ATTEMPT_API} from '../../quiz-attempt/application/attempt-api';
import {ACCOUNT_CONFIG} from '../application/account-config';
import {AccountHistoryPage} from './account-history.page';
import {AccountHistoryStore} from './account-history.store';

describe('AccountHistoryPage', () => {
    const entries = [
        {
            id: 'attempt-1',
            quizId: 'quiz-1',
            quizTitle: 'C# Căn bản & Nền tảng .NET',
            submittedAt: '2024-05-24T14:35:00Z',
            score: 82.5,
            passedScore: 70,
        },
        {
            id: 'attempt-2',
            quizId: 'quiz-2',
            quizTitle: 'Truy vấn dữ liệu với SQL',
            submittedAt: '2024-05-20T09:20:00Z',
            score: 65,
            passedScore: 70,
        },
    ];

    async function setup(history: unknown) {
        await TestBed.configureTestingModule({
            imports: [AccountHistoryPage],
            providers: [provideRouter([])],
        })
            .overrideComponent(AccountHistoryPage, {
                set: {
                    providers: [
                        {provide: ATTEMPT_API, useValue: {historyPage: history}},
                        {provide: ACCOUNT_CONFIG, useValue: {pageSize: 2}},
                        AccountHistoryStore,
                    ],
                },
            })
            .compileComponents();

        const fixture = TestBed.createComponent(AccountHistoryPage);
        fixture.detectChanges();
        await fixture.whenStable();
        fixture.detectChanges();

        return {fixture, element: fixture.nativeElement as HTMLElement};
    }

    it('renders one accessible row per submitted attempt', async () => {
        const {element} = await setup(
            vi.fn().mockResolvedValue({items: entries, totalCount: 4, pageNumber: 1, pageSize: 2}),
        );

        const rows = element.querySelectorAll('tbody tr');

        expect(rows).toHaveLength(2);
        expect(element.querySelector('tbody th[scope="row"]')?.textContent).toContain(
            'C# Căn bản & Nền tảng .NET',
        );
        expect(rows[0]!.querySelector('.history-table__score-badge')?.textContent).toContain('82,5%');
        expect(rows[0]!.querySelector('.history-table__link')?.getAttribute('href')).toBe('/quiz/quiz-1');
    });

    it('marks a passing attempt apart from a failing one', async () => {
        const {element} = await setup(
            vi.fn().mockResolvedValue({items: entries, totalCount: 2, pageNumber: 1, pageSize: 2}),
        );
        const rows = element.querySelectorAll('tbody tr');

        expect(rows[0]!.querySelector('.history-table__status')?.textContent).toContain('Đạt');
        expect(rows[1]!.querySelector('.history-table__status')?.textContent).toContain('Chưa đạt');
        expect(rows[1]!.querySelector('.history-table__score-badge--passed')).toBeNull();
    });

    it('reports the visible range and marks the current page', async () => {
        const {element} = await setup(
            vi.fn().mockResolvedValue({items: entries, totalCount: 4, pageNumber: 1, pageSize: 2}),
        );

        expect(element.querySelector('.pagination__count')?.textContent).toContain('1 - 2');
        expect(element.querySelector('.pagination__count')?.textContent).toContain('4');
        expect(
            element.querySelector('.pagination__pages [aria-current="page"]')?.textContent?.trim(),
        ).toBe('1');
    });

    it('requests the next page when paginating forward', async () => {
        const history = vi
            .fn()
            .mockResolvedValue({items: entries, totalCount: 4, pageNumber: 1, pageSize: 2});
        const {fixture, element} = await setup(history);

        const next = [...element.querySelectorAll<HTMLButtonElement>('.pagination__controls > button')].at(-1)!;
        next.click();
        fixture.detectChanges();
        await fixture.whenStable();
        fixture.detectChanges();

        expect(history).toHaveBeenLastCalledWith(2, 2);
    });

    it('invites the visitor to explore quizzes when nothing was submitted', async () => {
        const {element} = await setup(
            vi.fn().mockResolvedValue({items: [], totalCount: 0, pageNumber: 1, pageSize: 2}),
        );

        expect(element.querySelector('table')).toBeNull();
        expect(element.querySelector('.history-state')?.textContent).toContain(
            'Bạn chưa hoàn thành lượt làm bài nào.',
        );
        expect(element.querySelector('.history-state__cta')?.getAttribute('href')).toBe('/');
    });

    it('announces a load failure and offers a retry', async () => {
        const history = vi.fn().mockRejectedValue(new Error('offline'));
        const {fixture, element} = await setup(history);

        const alert = element.querySelector('[role="alert"]');
        expect(alert?.textContent).toContain('Không thể tải lịch sử làm bài');

        history.mockResolvedValue({items: entries, totalCount: 2, pageNumber: 1, pageSize: 2});
        element.querySelector<HTMLButtonElement>('.history-state__retry')!.click();
        fixture.detectChanges();
        await fixture.whenStable();
        fixture.detectChanges();

        expect(element.querySelectorAll('tbody tr')).toHaveLength(2);
    });
});
