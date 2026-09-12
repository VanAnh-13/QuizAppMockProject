import { computed, inject, Injectable, signal } from '@angular/core';
import { QUIZ_CATALOG } from '../data/quiz-catalog.provider';
import { QuizCategoryId } from '../domain/quiz-summary';

type QuizCategoryFilter = QuizCategoryId | 'all';

interface QuizCategoryFilterOption {
  readonly id: QuizCategoryFilter;
  readonly label: string;
}

@Injectable()
export class QuizExploreStore {
  private readonly catalog = inject(QUIZ_CATALOG);
  private readonly allQuizzes = this.catalog.listQuizzes();

  readonly categories: readonly QuizCategoryFilterOption[] = [
    { id: 'all', label: 'Tất cả' },
    ...this.catalog.listCategories(),
  ];
  readonly searchTerm = signal('');
  readonly selectedCategoryId = signal<QuizCategoryFilter>('all');
  readonly currentPage = signal(1);
  readonly pageSize = 6;
  readonly totalCatalogCount = this.allQuizzes.length;
  readonly categoryCount = this.catalog.listCategories().length;

  readonly filteredQuizzes = computed(() => {
    const normalizedSearch = this.searchTerm().trim().toLocaleLowerCase('vi-VN');
    const selectedCategoryId = this.selectedCategoryId();

    return this.allQuizzes.filter((quiz) => {
      const matchesCategory =
        selectedCategoryId === 'all' || quiz.categoryId === selectedCategoryId;
      const searchableContent = `${quiz.title} ${quiz.description} ${quiz.categoryLabel}`
        .toLocaleLowerCase('vi-VN');
      const matchesSearch = !normalizedSearch || searchableContent.includes(normalizedSearch);

      return matchesCategory && matchesSearch;
    });
  });

  readonly totalCount = computed(() => this.filteredQuizzes().length);
  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / this.pageSize)));
  readonly pageNumbers = computed(() =>
    Array.from({ length: this.totalPages() }, (_, index) => index + 1),
  );
  readonly visibleQuizzes = computed(() => {
    const startIndex = (this.currentPage() - 1) * this.pageSize;

    return this.filteredQuizzes().slice(startIndex, startIndex + this.pageSize);
  });
  readonly rangeStart = computed(() =>
    this.totalCount() === 0 ? 0 : (this.currentPage() - 1) * this.pageSize + 1,
  );
  readonly rangeEnd = computed(() =>
    Math.min(this.currentPage() * this.pageSize, this.totalCount()),
  );

  applySearch(searchTerm: string): void {
    this.searchTerm.set(searchTerm.trim());
    this.currentPage.set(1);
  }

  selectCategory(categoryId: QuizCategoryFilter): void {
    this.selectedCategoryId.set(categoryId);
    this.currentPage.set(1);
  }

  goToPage(pageNumber: number): void {
    const safePageNumber = Math.min(Math.max(pageNumber, 1), this.totalPages());
    this.currentPage.set(safePageNumber);
  }
}
