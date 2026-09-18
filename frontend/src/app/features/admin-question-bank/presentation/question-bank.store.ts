import {computed, inject, Injectable, signal} from '@angular/core';
import {apiErrorMessage} from '../../../core/api/api-error';
import {QUESTION_BANK_API} from '../application/question-bank-api';
import {QUESTION_BANK_CONFIG} from '../application/question-bank-config';
import {
    AdminQuestion,
    questionCode,
    questionLevelLabel,
    questionTypeIcon,
    questionTypeLabel,
} from '../domain/admin-question';
import {ConfirmationService} from '../../../shared/ui/confirmation/confirmation.service';

export interface QuestionBankRow {
    readonly id: string;
    readonly content: string;
    readonly code: string;
    readonly typeLabel: string;
    readonly typeIcon: string;
    readonly levelLabel: string;
    readonly level: number | null;
    readonly isActive: boolean;
    readonly statusLabel: string;
}

@Injectable()
export class QuestionBankStore {
    readonly pageSize = inject(QUESTION_BANK_CONFIG).pageSize;
    readonly totalCount = signal(0);
    readonly currentPage = signal(1);
    readonly searchTerm = signal('');
    readonly isLoading = signal(true);
    readonly isBusy = signal(false);
    readonly errorMessage = signal<string | null>(null);
    readonly totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / this.pageSize)));
    readonly pageNumbers = computed(() =>
        Array.from({length: this.totalPages()}, (_, index) => index + 1),
    );
    readonly rangeStart = computed(() =>
        this.totalCount() === 0 ? 0 : (this.currentPage() - 1) * this.pageSize + 1,
    );
    readonly hasPreviousPage = computed(() => this.currentPage() > 1);
    readonly hasNextPage = computed(() => this.currentPage() < this.totalPages());
    private readonly api = inject(QUESTION_BANK_API);
    private readonly confirmation = inject(ConfirmationService);
    private readonly questions = signal<readonly AdminQuestion[]>([]);
    readonly questionRows = computed<readonly QuestionBankRow[]>(() =>
        this.questions().map((question) => ({
            id: question.id,
            content: question.content,
            code: questionCode(question.id),
            typeLabel: questionTypeLabel(question.questionType),
            typeIcon: questionTypeIcon(question.questionType),
            levelLabel: questionLevelLabel(question.level),
            level: question.level,
            isActive: question.isActive,
            statusLabel: question.isActive ? 'Active' : 'Inactive',
        })),
    );
    readonly rows = this.questionRows;
    readonly rangeEnd = computed(() =>
        Math.min((this.currentPage() - 1) * this.pageSize + this.rows().length, this.totalCount()),
    );
    readonly selectedId = signal<string | null>(null);
    readonly selected = computed(() => this.rows().find((row) => row.id === this.selectedId()) ?? null);
    private loadVersion = 0;

    constructor() {
        void this.load(1);
    }

    select(row: QuestionBankRow): void {
        this.selectedId.set(row.id);
    }

    async load(pageNumber = this.currentPage()): Promise<void> {
        if (!Number.isInteger(pageNumber) || pageNumber < 1) return;

        const version = ++this.loadVersion;
        this.isLoading.set(true);
        this.errorMessage.set(null);

        try {
            const page = await this.api.list(pageNumber, this.pageSize, this.searchTerm());
            if (version !== this.loadVersion) return;

            this.questions.set(page.items);
            this.totalCount.set(page.totalCount);
            this.currentPage.set(pageNumber);

            const current = this.rows().find((r) => r.id === this.selectedId());
            if (current) {
                // keep current selected
            } else if (this.rows()[0]) {
                this.selectedId.set(this.rows()[0].id);
            } else {
                this.selectedId.set(null);
            }
        } catch (error) {
            if (version !== this.loadVersion) return;

            this.questions.set([]);
            this.selectedId.set(null);
            this.errorMessage.set(
                apiErrorMessage(error, 'Could not load the question bank. Try again.'),
            );
        } finally {
            if (version === this.loadVersion) this.isLoading.set(false);
        }
    }

    applySearch(search: string): void {
        this.searchTerm.set(search.trim());
        void this.load(1);
    }

    goToPage(pageNumber: number): void {
        if (!Number.isInteger(pageNumber)) return;

        const target = Math.min(Math.max(pageNumber, 1), this.totalPages());
        if (target === this.currentPage() && !this.isLoading()) return;

        void this.load(target);
    }

    previousPage(): void {
        if (this.hasPreviousPage()) this.goToPage(this.currentPage() - 1);
    }

    nextPage(): void {
        if (this.hasNextPage()) this.goToPage(this.currentPage() + 1);
    }

    async toggleActive(id: string): Promise<void> {
        const question = this.questions().find((item) => item.id === id);
        if (!question || this.isBusy()) return;

        const willDeactivate = question.isActive;
        const actionTitle = willDeactivate ? 'Are you sure to deactivate this question?' : 'Are you sure to activate this question?';
        const confirmed = await this.confirmation.confirm({
            title: actionTitle,
            message: `Are you sure you want to ${willDeactivate ? 'deactivate' : 'activate'} "${question.content.slice(0, 50)}..."?`,
            confirmLabel: 'Yes',
            cancelLabel: 'No',
            variant: willDeactivate ? 'warning' : 'primary',
            icon: willDeactivate ? 'pause_circle' : 'play_circle',
        });
        if (!confirmed) return;

        this.isBusy.set(true);
        try {
            await this.api.setActive(id, !question.isActive);
            await this.load(this.currentPage());
            void this.confirmation.notify({
                title: willDeactivate ? 'Question deactivated' : 'Question activated',
                message: 'Question status updated successfully.',
                variant: 'success',
                icon: willDeactivate ? 'pause_circle' : 'check_circle',
            });
        } catch (error) {
            this.errorMessage.set(
                apiErrorMessage(error, 'Could not update the question status. Try again.'),
            );
        } finally {
            this.isBusy.set(false);
        }
    }
}
