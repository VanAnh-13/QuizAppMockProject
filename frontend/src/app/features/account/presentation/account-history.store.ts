import {computed, inject, Injectable, signal} from '@angular/core';
import {apiErrorMessage} from '../../../core/api/api-error';
import {ACCOUNT_API} from '../application/account-api';
import {ACCOUNT_CONFIG} from '../application/account-config';
import {AttemptHistoryEntry, formatScore} from '../domain/account-contracts';

export interface AttemptHistoryRow {
    readonly id: string;
    readonly quizId: string;
    readonly quizTitle: string;
    readonly submittedAt: string;
    readonly score: string;
    readonly requirement: string | null;
    readonly passed: boolean;
}

@Injectable()
export class AccountHistoryStore {
    readonly pageSize = inject(ACCOUNT_CONFIG).pageSize;
    readonly totalCount = signal(0);
    readonly currentPage = signal(1);
    readonly isLoading = signal(true);
    readonly errorMessage = signal<string | null>(null);
    readonly totalPages = computed(() =>
        Math.max(1, Math.ceil(this.totalCount() / this.pageSize)),
    );
    readonly pageNumbers = computed(() =>
        Array.from({length: this.totalPages()}, (_, index) => index + 1),
    );
    readonly rangeStart = computed(() =>
        this.totalCount() === 0 ? 0 : (this.currentPage() - 1) * this.pageSize + 1,
    );
    readonly rangeEnd = computed(() =>
        Math.min((this.currentPage() - 1) * this.pageSize + this.historyRows().length, this.totalCount()),
    );
    readonly hasPreviousPage = computed(() => this.currentPage() > 1);
    readonly hasNextPage = computed(() => this.currentPage() < this.totalPages());
    private readonly api = inject(ACCOUNT_API);
    private readonly entries = signal<readonly AttemptHistoryEntry[]>([]);
    readonly historyRows = computed<readonly AttemptHistoryRow[]>(() =>
        this.entries().map((entry) => ({
            id: entry.id,
            quizId: entry.quizId,
            quizTitle: entry.quizTitle,
            submittedAt: entry.submittedAt,
            score: formatScore(entry.score),
            requirement: entry.passedScore === null ? null : formatScore(entry.passedScore),
            passed: entry.passedScore !== null && entry.score >= entry.passedScore,
        })),
    );
    private loadVersion = 0;

    constructor() {
        void this.load(1);
    }

    async load(pageNumber = this.currentPage()): Promise<void> {
        if (!Number.isInteger(pageNumber) || pageNumber < 1) return;

        const version = ++this.loadVersion;
        this.isLoading.set(true);
        this.errorMessage.set(null);

        try {
            const page = await this.api.history(pageNumber, this.pageSize);
            if (version !== this.loadVersion) return;

            this.entries.set(page.items);
            this.totalCount.set(page.totalCount);
            this.currentPage.set(pageNumber);
        } catch (error) {
            if (version !== this.loadVersion) return;

            this.entries.set([]);
            this.errorMessage.set(
                apiErrorMessage(error, 'Không thể tải lịch sử làm bài. Vui lòng thử lại.'),
            );
        } finally {
            if (version === this.loadVersion) this.isLoading.set(false);
        }
    }

    goToPage(pageNumber: number): void {
        if (!Number.isInteger(pageNumber)) return;

        const target = Math.min(Math.max(pageNumber, 1), this.totalPages());

        if (target === this.currentPage()) return;

        void this.load(target);
    }

    previousPage(): void {
        if (this.hasPreviousPage()) this.goToPage(this.currentPage() - 1);
    }

    nextPage(): void {
        if (this.hasNextPage()) this.goToPage(this.currentPage() + 1);
    }
}
