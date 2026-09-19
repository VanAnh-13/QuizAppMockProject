import {TestBed} from '@angular/core/testing';
import {PrototypeQuizCatalog} from '../infrastructure/prototype-quiz-catalog';
import {QUIZ_CATALOG} from '../infrastructure/quiz-catalog.provider';
import {QUIZ_EXPLORE_CONFIG} from './quiz-explore.config';
import {QuizExploreStore} from './quiz-explore.store';

const unicodeCatalog = {
    listQuizzes: async () => [{
        id: 'unicode-quiz', categoryId: 'general' as const, categoryLabel: 'General',
        title: 'Điều hướng', description: 'Kiểu giá trị', questionCount: 1,
        durationMinutes: 5, status: 'open' as const,
    }],
};

async function createStore(pageSize = 6, catalog = new PrototypeQuizCatalog()): Promise<QuizExploreStore> {
    TestBed.configureTestingModule({
        providers: [
            {provide: QUIZ_CATALOG, useValue: catalog},
            QuizExploreStore,
            {provide: QUIZ_EXPLORE_CONFIG, useValue: {pageSize}},
        ],
    });
    const store = TestBed.inject(QuizExploreStore);
    await store.refresh();
    return store;
}

describe('QuizExploreStore', () => {
    it('derives catalog totals from the provided data', async () => {
        const store = await createStore();

        expect(store.totalCatalogCount()).toBe(6);
        expect(store.categoryCount()).toBe(5);
        expect(store.visibleQuizzes()).toHaveLength(6);
        expect(store.isLoading()).toBe(false);
        expect(store.errorMessage()).toBeNull();
    });

    it('filters by category and resets a real second page', async () => {
        const store = await createStore(4);
        store.goToPage(2);
        expect(store.currentPage()).toBe(2);

        store.selectCategory('csharp');

        expect(store.currentPage()).toBe(1);
        expect(store.visibleQuizzes().map((quiz) => quiz.id)).toEqual([
            'csharp-co-ban-oop',
            'csharp-linq-collection-queries',
        ]);
    });

    it.each([' SQL ', 'sql', 'SqL'])(
        'searches without case or surrounding whitespace: %s',
        async (term) => {
            const store = await createStore();
            store.applySearch(term);
            expect(store.visibleQuizzes().map((quiz) => quiz.id)).toEqual(['sql-server-fundamentals']);
        },
    );

    it.each(['kieu gia tri', 'KIỂU GIÁ TRỊ', 'kiểu giá trị'])(
        'matches Vietnamese accents and decomposed Unicode: %s',
        async (term) => {
            const store = await createStore(6, unicodeCatalog);
            store.applySearch(term);
            expect(store.visibleQuizzes()[0]?.id).toBe('unicode-quiz');
        },
    );

    it('normalizes the Vietnamese letter đ', async () => {
        const store = await createStore(6, unicodeCatalog);
        store.applySearch('dieu huong');
        expect(store.visibleQuizzes()[0]?.id).toBe('unicode-quiz');
    });

    it('combines a category with the search term', async () => {
        const store = await createStore();
        store.selectCategory('csharp');
        store.applySearch('linq');
        expect(store.totalCount()).toBe(1);
        expect(store.visibleQuizzes()[0]?.id).toBe('csharp-linq-collection-queries');
    });

    it('reports zero ranges when nothing matches', async () => {
        const store = await createStore();
        store.applySearch('không có quizzes này');
        expect(store.visibleQuizzes()).toEqual([]);
        expect(store.totalCount()).toBe(0);
        expect(store.rangeStart()).toBe(0);
        expect(store.rangeEnd()).toBe(0);
    });

    it('resets both filters and pagination', async () => {
        const store = await createStore(1);
        store.selectCategory('csharp');
        store.goToPage(2);
        store.applySearch('linq');
        store.resetFilters();
        expect(store.searchTerm()).toBe('');
        expect(store.selectedCategoryId()).toBe('all');
        expect(store.currentPage()).toBe(1);
        expect(store.totalCount()).toBe(store.totalCatalogCount());
    });

    it('uses configured page size and handles a partially filled last page', async () => {
        const store = await createStore(4);
        expect(store.pageNumbers()).toEqual([1, 2]);
        expect(store.visibleQuizzes()).toHaveLength(4);
        store.goToPage(2);
        expect(store.visibleQuizzes()).toHaveLength(2);
        expect(store.rangeStart()).toBe(5);
        expect(store.rangeEnd()).toBe(6);
    });

    it('clamps navigation to the available pages', async () => {
        const store = await createStore(4);
        store.goToPage(999);
        expect(store.currentPage()).toBe(2);
        store.goToPage(-1);
        expect(store.currentPage()).toBe(1);
    });

    it.each([NaN, Infinity, 1.5])('ignores non-integer page requests: %s', async (page) => {
        const store = await createStore(4);
        store.goToPage(page);
        expect(store.currentPage()).toBe(1);
        expect(store.visibleQuizzes()).toHaveLength(4);
    });

    it('returns to page one when searching from a later page', async () => {
        const store = await createStore(4);
        store.goToPage(2);
        store.applySearch('SQL');
        expect(store.currentPage()).toBe(1);
        expect(store.rangeStart()).toBe(1);
        expect(store.rangeEnd()).toBe(1);
    });
});
