import {computed, inject, Injectable, signal} from '@angular/core';
import {FormControl, FormGroup, Validators} from '@angular/forms';
import {debounceTime} from 'rxjs';
import {apiErrorMessage} from '../../../core/api/api-error';
import {ADMIN_QUIZ_API, ADMIN_QUIZ_CONFIG} from '../application/admin-quiz-api';
import {AdminQuiz} from '../domain/admin-quiz';
import {ConfirmationService} from '../../../shared/ui/confirmation/confirmation.service';

@Injectable()
export class QuizAdminStore {
    readonly searchControl = new FormControl('', {nonNullable: true});
    readonly form = new FormGroup({
        title: new FormControl('', {nonNullable: true, validators: [Validators.required]}),
        description: new FormControl('', {nonNullable: true}),
        duration: new FormControl(15, {nonNullable: true, validators: [Validators.min(1)]}),
        image: new FormControl('', {nonNullable: true}),
        passedScore: new FormControl(70, {nonNullable: true, validators: [Validators.min(0), Validators.max(100)]}),
        isActive: new FormControl(true, {nonNullable: true}),
    });
    readonly pageSize = inject(ADMIN_QUIZ_CONFIG).pageSize;
    readonly quizzes = signal<readonly AdminQuiz[]>([]);
    readonly totalCount = signal(0);
    readonly currentPage = signal(1);
    readonly selectedId = signal<string | null>(null);
    readonly isLoading = signal(true);
    readonly isSaving = signal(false);
    readonly errorMessage = signal<string | null>(null);
    readonly selected = computed(() => this.quizzes().find((quiz) => quiz.id === this.selectedId()) ?? null);
    readonly rangeStart = computed(() => (this.totalCount() === 0 ? 0 : (this.currentPage() - 1) * this.pageSize + 1));
    readonly rangeEnd = computed(() =>
        Math.min((this.currentPage() - 1) * this.pageSize + this.quizzes().length, this.totalCount()),
    );
    readonly hasPreviousPage = computed(() => this.currentPage() > 1);
    readonly hasNextPage = computed(() => this.currentPage() * this.pageSize < this.totalCount());
    private readonly api = inject(ADMIN_QUIZ_API);
    private readonly confirmation = inject(ConfirmationService);

    constructor() {
        this.searchControl.valueChanges.pipe(debounceTime(250)).subscribe(() => void this.load(1));
        void this.load(1);
    }

    async load(pageNumber = this.currentPage()): Promise<void> {
        this.isLoading.set(true);
        this.errorMessage.set(null);
        try {
            const page = await this.api.list(pageNumber, this.pageSize, this.searchControl.value.trim());
            this.quizzes.set(page.items);
            this.totalCount.set(page.totalCount);
            this.currentPage.set(pageNumber);
            if (!this.selected() && page.items[0]) this.select(page.items[0]);
        } catch (error) {
            this.quizzes.set([]);
            this.errorMessage.set(apiErrorMessage(error, 'Could not load quizzes.'));
        } finally {
            this.isLoading.set(false);
        }
    }

    select(quiz: AdminQuiz | null): void {
        this.selectedId.set(quiz?.id ?? null);
        this.form.reset({
            title: quiz?.title ?? '',
            description: quiz?.description ?? '',
            duration: quiz?.duration ?? 15,
            image: quiz?.image ?? '',
            passedScore: quiz?.passedScore ?? 70,
            isActive: quiz?.isActive ?? true,
        });
    }

    startCreate(): void {
        this.select(null);
    }

    async save(): Promise<void> {
        this.form.markAllAsTouched();
        if (this.form.invalid) {
            this.errorMessage.set('Quiz details are incomplete.');
            return;
        }
        const value = this.form.getRawValue();
        const draft = {
            title: value.title.trim(),
            description: value.description.trim() || null,
            duration: value.duration,
            image: value.image.trim() || null,
            passedScore: value.passedScore,
            isActive: value.isActive,
        };

        const isEditing = !!this.selected();
        const actionTitle = isEditing ? 'Are you sure to save changes to this quiz?' : 'Are you sure to create this quiz?';
        const confirmed = await this.confirmation.confirm({
            title: actionTitle,
            message: `Save details for quiz "${draft.title}"?`,
            confirmLabel: 'Yes',
            cancelLabel: 'No',
            variant: 'primary',
            icon: 'help_outline',
        });
        if (!confirmed) return;

        this.isSaving.set(true);
        this.errorMessage.set(null);
        try {
            const selected = this.selected();
            if (selected) await this.api.update(selected.id, draft);
            else await this.api.create(draft);
            await this.load(this.currentPage());
            void this.confirmation.notify({
                title: 'Quiz saved',
                message: `The quiz "${draft.title}" has been saved successfully.`,
                variant: 'success',
                icon: 'check_circle',
            });
        } catch (error) {
            this.errorMessage.set(apiErrorMessage(error, 'Could not save the quiz.'));
        } finally {
            this.isSaving.set(false);
        }
    }

    async toggleActive(quiz: AdminQuiz): Promise<void> {
        const willDeactivate = quiz.isActive;
        const confirmed = await this.confirmation.confirm({
            title: willDeactivate ? 'Are you sure to deactivate this quiz?' : 'Are you sure to activate this quiz?',
            message: `Are you sure you want to ${willDeactivate ? 'deactivate' : 'activate'} "${quiz.title}"?`,
            confirmLabel: 'Yes',
            cancelLabel: 'No',
            variant: willDeactivate ? 'warning' : 'primary',
            icon: willDeactivate ? 'pause_circle' : 'play_circle',
        });
        if (!confirmed) return;

        try {
            await this.api.setActive(quiz.id, !quiz.isActive);
            await this.load(this.currentPage());
            void this.confirmation.notify({
                title: willDeactivate ? 'Quiz deactivated' : 'Quiz activated',
                message: `"${quiz.title}" is now ${willDeactivate ? 'inactive' : 'active'}.`,
                variant: 'success',
                icon: willDeactivate ? 'pause_circle' : 'check_circle',
            });
        } catch (error) {
            this.errorMessage.set(apiErrorMessage(error, `Could not ${willDeactivate ? 'deactivate' : 'activate'} quiz.`));
        }
    }
}
