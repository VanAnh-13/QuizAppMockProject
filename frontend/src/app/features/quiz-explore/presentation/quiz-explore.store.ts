import { computed, inject, Injectable, signal } from '@angular/core';
import { QUIZ_CATALOG } from '../infrastructure/quiz-catalog.provider';
import { QuizCategoryId, QuizSummary } from '../domain/quiz-summary';
import { QUIZ_EXPLORE_CONFIG } from './quiz-explore.config';
import { apiErrorMessage } from '../../../core/api/api-error';

type QuizCategoryFilter = QuizCategoryId | 'general' | 'all';

interface QuizCategoryFilterOption {
  readonly id: QuizCategoryFilter;
  readonly label: string;
}

@Injectable()
export class QuizExploreStore {
  private readonly catalog = inject(QUIZ_CATALOG);
  private readonly allQuizzes = signal<readonly QuizSummary[]>([]);
  private loadVersion = 0;

  readonly categories = computed<readonly QuizCategoryFilterOption[]>(() => {
    const categoryMap = new Map<QuizSummary['categoryId'], string>();
    for (const quiz of this.allQuizzes())
      categoryMap.set(quiz.categoryId, categoryLabels[quiz.categoryId]);

    return [
      { id: 'all', label: 'Tất cả' },
      ...Array.from(categoryMap, ([id, label]) => ({ id, label })),
    ];
  });
  readonly searchTerm = signal('');
  readonly selectedCategoryId = signal<QuizCategoryFilter>('all');
  readonly currentPage = signal(1);
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly pageSize = inject(QUIZ_EXPLORE_CONFIG).pageSize;
  readonly totalCatalogCount = computed(() => this.allQuizzes().length);
  readonly categoryCount = computed(
    () => new Set(this.allQuizzes().map((quiz) => quiz.categoryId)).size,
  );

  constructor() {
    void this.refresh();
  }

  async refresh(): Promise<void> {
    const version = ++this.loadVersion;
    this.isLoading.set(true);
    this.errorMessage.set(null);

    try {
      const quizzes = await this.catalog.listQuizzes();
      if (version !== this.loadVersion) return;
      this.allQuizzes.set(quizzes);
      this.goToPage(this.currentPage());
    } catch (error) {
      if (version !== this.loadVersion) return;
      this.allQuizzes.set([]);
      this.errorMessage.set(
        apiErrorMessage(error, 'Không thể tải danh sách quiz từ máy chủ. Vui lòng thử lại.'),
      );
    } finally {
      if (version === this.loadVersion) this.isLoading.set(false);
    }
  }

  readonly filteredQuizzes = computed(() => {
    const normalizedSearch = normalizeSearch(this.searchTerm());
    const selectedCategoryId = this.selectedCategoryId();

    return this.allQuizzes().filter((quiz) => {
      const matchesCategory =
        selectedCategoryId === 'all' || quiz.categoryId === selectedCategoryId;
      const searchableContent = normalizeSearch(
        `${quiz.title} ${quiz.description} ${quiz.categoryLabel}`,
      );
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
    if (!Number.isInteger(pageNumber)) {
      return;
    }

    const safePageNumber = Math.min(Math.max(pageNumber, 1), this.totalPages());
    this.currentPage.set(safePageNumber);
  }

  resetFilters(): void {
    this.searchTerm.set('');
    this.selectedCategoryId.set('all');
    this.currentPage.set(1);
  }
}

const categoryLabels: Readonly<Record<QuizSummary['categoryId'], string>> = {
  general: 'Kiến thức tổng hợp',
  csharp: 'C#/.NET',
  'sql-server': 'SQL Server',
  angular: 'Angular',
  typescript: 'TypeScript',
  api: 'Lập trình web',
};

function normalizeSearch(value: string): string {
  return value
    .trim()
    .toLocaleLowerCase('vi-VN')
    .normalize('NFD')
    .replace(/\p{M}/gu, '')
    .replace(/đ/g, 'd');
}
