import {
    ChangeDetectionStrategy,
    Component,
    HostListener,
    inject,
    OnDestroy,
    signal,
} from '@angular/core';
import {ActivatedRoute, RouterLink} from '@angular/router';
import {Location} from '@angular/common';
import {attemptContentProvider} from '../infrastructure/attempt-content.provider';
import {ATTEMPT_CONFIG} from '../application/attempt-config';
import {QuizAttemptStore} from './quiz-attempt.store';
import {AuthDialogComponent} from '../../../shared/ui/auth-dialog/auth-dialog.component';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [RouterLink, AuthDialogComponent],
    providers: [attemptContentProvider, QuizAttemptStore],
    selector: 'app-quiz-attempt-page',
    styleUrls: ['./quiz-attempt.page.css', './quiz-attempt.sidebar.css', './quiz-attempt.dialog.css'],
    templateUrl: './quiz-attempt.page.html',
})
export class QuizAttemptPage implements OnDestroy {
    protected readonly store = inject(QuizAttemptStore);
    protected readonly submitDialogOpen = signal(false);
    private readonly route = inject(ActivatedRoute);
    private readonly location = inject(Location);
    private readonly timerId = setInterval(() => this.store.tick(), inject(ATTEMPT_CONFIG).tickMs);
    private readonly subscription = this.route.paramMap.subscribe(() => {
        void this.load();
    });

    protected async load(): Promise<void> {
        const quizId = this.route.snapshot.paramMap.get('quizId');

        if (!quizId) return;

        await this.store.load(
            quizId,
            this.store.attemptId() ?? this.route.snapshot.queryParamMap.get('attemptId'),
        );

        if (this.store.attemptId())
            this.location.replaceState(
                `/quiz/${encodeURIComponent(quizId)}/attempt`,
                `attemptId=${encodeURIComponent(this.store.attemptId()!)}`,
            );
    }

    protected async reload(): Promise<void> {
        if (
            this.store.dirty() &&
            !window.confirm(
                'Tải lại sẽ thay đáp án chưa lưu bằng bản trên máy chủ. Bạn có muốn tiếp tục?',
            )
        )
            return;
        await this.load();
    }

    async canLeave(): Promise<boolean> {
        if (this.store.isBusy()) return false;
        if (!this.store.dirty() && !this.store.isSaving()) return true;
        if (await this.store.save()) return true;
        return window.confirm('Có đáp án chưa lưu thành công. Bạn vẫn muốn rời trang?');
    }

    ngOnDestroy(): void {
        clearInterval(this.timerId);
        this.subscription.unsubscribe();
    }

    protected toggleOption(id: string): void {
        this.store.toggleOption(id);
    }

    protected openSubmitConfirmation(): void {
        if (!this.store.isBusy() && !this.store.result()) this.submitDialogOpen.set(true);
    }

    protected closeSubmitConfirmation(): void {
        if (!this.store.isBusy()) this.submitDialogOpen.set(false);
    }

    protected async confirmSubmission(): Promise<void> {
        if (await this.store.submit()) this.submitDialogOpen.set(false);
    }

    @HostListener('window:beforeunload', ['$event'])
    protected beforeUnload(event: BeforeUnloadEvent): void {
        if (this.store.dirty() || this.store.isSaving() || this.store.isBusy()) event.preventDefault();
    }

    @HostListener('window:keydown', ['$event'])
    protected navigateWithArrowKeys(event: KeyboardEvent): void {
        const target = event.target;

        if (
            this.submitDialogOpen() ||
            event.altKey ||
            event.ctrlKey ||
            event.metaKey ||
            (target instanceof HTMLElement &&
                target.closest('input, textarea, select, [contenteditable="true"], dialog'))
        )
            return;

        if (event.key === 'ArrowLeft') this.store.previousQuestion();
        else if (event.key === 'ArrowRight') this.store.nextQuestion();
        else return;

        event.preventDefault();
    }
}
