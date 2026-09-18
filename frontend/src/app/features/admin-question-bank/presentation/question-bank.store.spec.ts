import {HttpErrorResponse} from '@angular/common/http';
import {TestBed} from '@angular/core/testing';
import {QUESTION_BANK_API} from '../application/question-bank-api';
import {QUESTION_BANK_CONFIG} from '../application/question-bank-config';
import {AdminQuestion, QuestionLevel, QuestionType} from '../domain/admin-question';
import {QuestionBankStore} from './question-bank.store';
import {ConfirmationService} from '../../../shared/ui/confirmation/confirmation.service';
import {MockConfirmationService} from '../../../../testing/mock-confirmation';

function question(id: string, overrides: Partial<AdminQuestion> = {}): AdminQuestion {
    return {
        id,
        content: `Nội dung ${id}`,
        image: null,
        level: QuestionLevel.Easy,
        questionType: QuestionType.SingleChoice,
        isActive: true,
        ...overrides,
    };
}

describe('QuestionBankStore', () => {
    function setup(totalCount = 8, pageSize = 6) {
        const list = vi.fn(async (pageNumber: number, size: number) => {
            const remaining = Math.max(0, totalCount - (pageNumber - 1) * size);
            return {
                items: Array.from({length: Math.min(size, remaining)}, (_, index) =>
                    question(`${pageNumber}-${index}`),
                ),
                totalCount,
                pageNumber,
                pageSize: size,
            };
        });
        const setActive = vi.fn().mockResolvedValue(undefined);
        TestBed.configureTestingModule({
            providers: [
                QuestionBankStore,
                {provide: ConfirmationService, useClass: MockConfirmationService},
                {provide: QUESTION_BANK_API, useValue: {list, setActive}},
                {provide: QUESTION_BANK_CONFIG, useValue: {pageSize}},
            ],
        });

        return {store: TestBed.inject(QuestionBankStore), list, setActive};
    }

    it('loads the first page and maps Vietnamese labels', async () => {
        const {store, list} = setup();
        await store.load(1);

        expect(list).toHaveBeenCalledWith(1, 6, '');
        expect(store.rows()).toHaveLength(6);
        expect(store.rows()[0].typeLabel).toBe('Single choice');
        expect(store.rows()[0].statusLabel).toBe('Active');
        expect(store.selectedId()).toBe('1-0');
        expect(store.selected()?.id).toBe('1-0');
        expect(store.rangeStart()).toBe(1);
        expect(store.rangeEnd()).toBe(6);
        expect(store.totalPages()).toBe(2);
    });

    it('selects a different question row', async () => {
        const {store} = setup();
        await store.load(1);

        store.select(store.rows()[2]);
        expect(store.selectedId()).toBe('1-2');
        expect(store.selected()?.id).toBe('1-2');
    });

    it('searches from the first page', async () => {
        const {store, list} = setup();
        await store.load(1);
        list.mockClear();

        store.applySearch('  OOP  ');
        await vi.waitUntil(() => !store.isLoading());

        expect(list).toHaveBeenCalledWith(1, 6, 'OOP');
        expect(store.searchTerm()).toBe('OOP');
        expect(store.currentPage()).toBe(1);
    });

    it('toggles activation then reloads the current page', async () => {
        const {store, list, setActive} = setup(1);
        await store.load(1);
        list.mockClear();

        await store.toggleActive(store.rows()[0].id);

        expect(setActive).toHaveBeenCalledWith('1-0', false);
        expect(list).toHaveBeenCalledWith(1, 6, '');
    });

    it('surfaces a friendly message when loading fails', async () => {
        const {store} = setup();
        await store.load(1);
        TestBed.inject(QUESTION_BANK_API).list = vi
            .fn()
            .mockRejectedValue(new HttpErrorResponse({status: 403}));

        await store.load(1);

        expect(store.rows()).toEqual([]);
        expect(store.errorMessage()).toBe('Your account does not have permission to access this content.');
    });
});
