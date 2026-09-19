import {HttpErrorResponse} from '@angular/common/http';
import {TestBed} from '@angular/core/testing';
import {ATTEMPT_API, AttemptResult} from '../../quiz-attempt/application/attempt-api';
import {ACCOUNT_CONFIG} from '../application/account-config';
import {AccountHistoryStore} from './account-history.store';

function entry(id: string, score = 80, passedScore: number | null = 70): AttemptResult {
    return {
        id,
        quizId: `quiz-${id}`,
        quizTitle: `Quiz ${id}`,
        submittedAt: '2024-05-24T14:35:00Z',
        score,
        passedScore,
    };
}

describe('AccountHistoryStore', () => {
    function setup(totalCount = 12, pageSize = 8) {
        const history = vi.fn(async (pageNumber: number, size: number) => {
            const remaining = Math.max(0, totalCount - (pageNumber - 1) * size);

            return {
                items: Array.from({length: Math.min(size, remaining)}, (_, index) =>
                    entry(`${pageNumber}-${index}`),
                ),
                totalCount,
                pageNumber,
                pageSize: size,
            };
        });
        TestBed.configureTestingModule({
            providers: [
                AccountHistoryStore,
                {provide: ATTEMPT_API, useValue: {historyPage: history}},
                {provide: ACCOUNT_CONFIG, useValue: {pageSize}},
            ],
        });

        return {store: TestBed.inject(AccountHistoryStore), history};
    }

    it('loads the first page and derives the visible range', async () => {
        const {store, history} = setup();
        await store.load(1);

        expect(history).toHaveBeenCalledWith(1, 8);
        expect(store.historyRows()).toHaveLength(8);
        expect(store.totalCount()).toBe(12);
        expect(store.totalPages()).toBe(2);
        expect(store.pageNumbers()).toEqual([1, 2]);
        expect(store.rangeStart()).toBe(1);
        expect(store.rangeEnd()).toBe(8);
        expect(store.isLoading()).toBe(false);
        expect(store.errorMessage()).toBeNull();
    });

    it('requests the server page and reports a partial final page', async () => {
        const {store, history} = setup();
        await store.load(1);

        store.goToPage(2);
        await vi.waitUntil(() => !store.isLoading());

        expect(history).toHaveBeenLastCalledWith(2, 8);
        expect(store.currentPage()).toBe(2);
        expect(store.historyRows()).toHaveLength(4);
        expect(store.rangeStart()).toBe(9);
        expect(store.rangeEnd()).toBe(12);
        expect(store.hasNextPage()).toBe(false);
        expect(store.hasPreviousPage()).toBe(true);
    });

    it('retries the failed destination rather than the previously displayed page', async () => {
        const {store, history} = setup();
        await store.load(1);
        history.mockRejectedValueOnce(new HttpErrorResponse({status: 500}));

        store.goToPage(2);
        await vi.waitUntil(() => !store.isLoading());

        expect(store.errorMessage()).not.toBeNull();
        expect(store.currentPage()).toBe(1);
        await store.load();

        expect(history).toHaveBeenLastCalledWith(2, 8);
        expect(store.currentPage()).toBe(2);
        expect(store.historyRows()).toHaveLength(4);
        expect(store.rangeStart()).toBe(9);
        expect(store.errorMessage()).toBeNull();
    });

    it('does not replace the retry destination with an invalid page request', async () => {
        const {store, history} = setup();
        await store.load(1);
        history.mockRejectedValueOnce(new HttpErrorResponse({status: 500}));
        await store.load(2);

        await store.load(NaN);
        await store.load(0);
        await store.load();

        expect(history).toHaveBeenLastCalledWith(2, 8);
        expect(store.currentPage()).toBe(2);
    });

    it('clamps navigation to the available pages', async () => {
        const {store} = setup();
        await store.load(1);

        store.goToPage(999);
        await vi.waitUntil(() => !store.isLoading());

        expect(store.currentPage()).toBe(2);
    });

    it.each([NaN, Infinity, 1.5, 0, -1])('ignores an invalid page request %s', async (page) => {
        const {store, history} = setup();
        await store.load(1);
        history.mockClear();

        store.goToPage(page);

        expect(history).not.toHaveBeenCalled();
        expect(store.currentPage()).toBe(1);
    });

    it('does not refetch the page already shown', async () => {
        const {store, history} = setup();
        await store.load(1);
        history.mockClear();

        store.goToPage(1);

        expect(history).not.toHaveBeenCalled();
    });

    it('reports an empty history without a page range', async () => {
        const {store} = setup(0);
        await store.load(1);

        expect(store.historyRows()).toEqual([]);
        expect(store.totalPages()).toBe(1);
        expect(store.rangeStart()).toBe(0);
        expect(store.rangeEnd()).toBe(0);
        expect(store.hasNextPage()).toBe(false);
    });

    it('surfaces a friendly message and clears rows when loading fails', async () => {
        const {store} = setup();
        await store.load(1);
        TestBed.inject(ATTEMPT_API).historyPage = vi
            .fn()
            .mockRejectedValue(new HttpErrorResponse({status: 500}));

        await store.load(1);

        expect(store.historyRows()).toEqual([]);
        expect(store.errorMessage()).toBe(
            'Không thể tải lịch sử làm bài. Vui lòng thử lại.',
        );
        expect(store.isLoading()).toBe(false);
    });

    it('marks a row as passed only when it reaches the pass mark', async () => {
        const {store, history} = setup();
        history.mockResolvedValue({
            items: [
                entry('a', 82.5, 70),
                entry('b', 70, 70),
                entry('c', 65, 70),
                entry('d', 65, null),
            ],
            totalCount: 4,
            pageNumber: 1,
            pageSize: 8,
        });

        await store.load(1);

        const rows = store.historyRows();
        expect(rows.map((row) => row.passed)).toEqual([true, true, false, false]);
    });

    it('formats scores with the Vietnamese decimal separator', async () => {
        const {store, history} = setup();
        history.mockResolvedValue({
            items: [entry('a', 82.5), entry('b', 75), entry('c', 66.666)],
            totalCount: 3,
            pageNumber: 1,
            pageSize: 8,
        });

        await store.load(1);

        expect(store.historyRows().map((row) => row.score)).toEqual(['82,5', '75', '66,7']);
    });
});
