import {
    ChangeDetectionStrategy,
    Component,
    effect,
    ElementRef,
    HostListener,
    inject,
    OnDestroy,
    signal,
    viewChild,
} from '@angular/core';
import {ActivatedRoute, RouterLink} from '@angular/router';
import {Location} from '@angular/common';
import {ConfirmationService} from '../../../shared/ui/confirmation/confirmation.service';
import {provideQuizAttempt} from '../infrastructure/attempt-content.provider';
import {ATTEMPT_CONFIG} from '../application/attempt-config';
import {QuizAttemptStore} from './quiz-attempt.store';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [RouterLink],
    providers: [provideQuizAttempt(), QuizAttemptStore],
    selector: 'app-quiz-attempt-page',
    styleUrls: ['./quiz-attempt.page.css', './quiz-attempt.sidebar.css', './quiz-attempt.dialog.css'],
    templateUrl: './quiz-attempt.page.html',
})
export class QuizAttemptPage implements OnDestroy {
    protected readonly store = inject(QuizAttemptStore);
    protected readonly submitDialogOpen = signal(false);
    protected readonly leaveDialogOpen = signal(false);
    protected readonly leaveMessage = signal('Bạn đang làm bài. Bạn có chắc muốn rời trang?');
    private readonly submitDialog = viewChild<ElementRef<HTMLDialogElement>>('submitDialog');
    private readonly leaveDialog = viewChild<ElementRef<HTMLDialogElement>>('leaveDialog');
    private leaveResolver: ((value: boolean) => void) | null = null;
    private destroyed = false;
    private readonly confirmation = inject(ConfirmationService);
    private readonly route = inject(ActivatedRoute);
    private readonly location = inject(Location);
    private readonly timerId = setInterval(() => this.store.tick(), inject(ATTEMPT_CONFIG).tickMs);
    private readonly subscription = this.route.paramMap.subscribe(() => {
        void this.load();
    });

    constructor() {
        effect(() => {
            if (this.store.result()) {
                this.submitDialog()?.nativeElement.close();
                this.submitDialogOpen.set(false);
                this.closeLeaveConfirmation();
            }
        });
    }

    protected async load(): Promise<void> {
        if (this.destroyed) return;

        const quizId = this.route.snapshot.paramMap.get('quizId');

        if (!quizId) return;

        await this.store.load(
            quizId,
            this.store.attemptId() ?? this.route.snapshot.queryParamMap.get('attemptId'),
        );

        if (this.destroyed) return;

        if (this.store.attemptId())
            this.location.replaceState(
                `/quiz/${encodeURIComponent(quizId)}/attempt`,
                `attemptId=${encodeURIComponent(this.store.attemptId()!)}`,
            );
    }

    protected async reload(): Promise<void> {
        if (
            this.store.dirty() &&
            !(await this.confirmation.confirm({
                title: 'Tải lại trạng thái?',
                message: 'Tải lại sẽ thay đáp án chưa lưu bằng bản trên máy chủ. Bạn có muốn tiếp tục?',
                confirmLabel: 'Tải lại',
                cancelLabel: 'Hủy',
            }))
        )
            return;
        if (this.destroyed) return;
        await this.load();
    }

    protected loginReturnUrl(): string {
        return this.location.path();
    }

    async canLeave(): Promise<boolean> {
        if (this.store.isBusy()) return false;

        if (this.store.result() || !this.store.attemptId()) return true;

        if (this.store.dirty() || this.store.isSaving()) {
            if (await this.store.save()) {
                return this.promptLeaveConfirmation('Bạn đang làm bài. Bạn có chắc muốn rời trang?');
            }
            return this.promptLeaveConfirmation('Có đáp án chưa lưu thành công. Bạn vẫn muốn rời trang?');
        }

        return this.promptLeaveConfirmation('Bạn đang làm bài. Bạn có chắc muốn rời trang?');
    }

    private promptLeaveConfirmation(message: string): Promise<boolean> {
        if (this.leaveResolver) {
            this.leaveResolver(false);
            this.leaveResolver = null;
        }

        const dialog = this.leaveDialog()?.nativeElement;
        if (!dialog) {
            return Promise.resolve(false);
        }

        this.leaveMessage.set(message);
        if (!dialog.open) {
            dialog.showModal();
        }
        this.leaveDialogOpen.set(true);

        return new Promise<boolean>((resolve) => {
            this.leaveResolver = resolve;
        });
    }

    protected confirmLeave(): void {
        const resolver = this.leaveResolver;
        this.leaveResolver = null;
        this.closeLeaveConfirmation();
        resolver?.(true);
    }

    protected cancelLeave(): void {
        const resolver = this.leaveResolver;
        this.leaveResolver = null;
        this.closeLeaveConfirmation();
        resolver?.(false);
    }

    protected cancelLeaveConfirmation(event: Event): void {
        event.preventDefault();
        this.cancelLeave();
    }

    protected onLeaveDialogClose(): void {
        this.leaveDialogOpen.set(false);
        if (this.leaveResolver) {
            const resolver = this.leaveResolver;
            this.leaveResolver = null;
            resolver(false);
        }
    }

    protected dismissLeaveBackdrop(event: MouseEvent): void {
        const dialog = this.leaveDialog()?.nativeElement;
        if (!dialog || event.target !== dialog) return;

        const bounds = dialog.getBoundingClientRect();
        if (
            event.clientX < bounds.left ||
            event.clientX > bounds.right ||
            event.clientY < bounds.top ||
            event.clientY > bounds.bottom
        ) {
            this.cancelLeave();
        }
    }

    private closeLeaveConfirmation(): void {
        const dialog = this.leaveDialog()?.nativeElement;
        if (dialog?.open) {
            dialog.close();
        }
        this.leaveDialogOpen.set(false);
    }

    ngOnDestroy(): void {
        this.destroyed = true;
        this.confirmation.cancel();
        clearInterval(this.timerId);
        this.subscription.unsubscribe();
        if (this.leaveResolver) {
            const resolver = this.leaveResolver;
            this.leaveResolver = null;
            resolver(false);
        }
    }

    protected toggleOption(id: string): void {
        this.store.toggleOption(id);
    }

    protected openSubmitConfirmation(): void {
        if (this.store.isBusy() || this.store.result()) return;

        const dialog = this.submitDialog()?.nativeElement;
        if (!dialog || dialog.open) return;

        dialog.showModal();
        this.submitDialogOpen.set(true);
    }

    protected closeSubmitConfirmation(): void {
        if (this.store.isBusy()) return;

        this.submitDialog()?.nativeElement.close();
        this.submitDialogOpen.set(false);
    }

    protected cancelSubmitConfirmation(event: Event): void {
        event.preventDefault();
        this.closeSubmitConfirmation();
    }

    protected dismissSubmitBackdrop(event: MouseEvent): void {
        const dialog = this.submitDialog()?.nativeElement;
        if (!dialog || event.target !== dialog) return;

        const bounds = dialog.getBoundingClientRect();
        if (
            event.clientX < bounds.left ||
            event.clientX > bounds.right ||
            event.clientY < bounds.top ||
            event.clientY > bounds.bottom
        ) {
            this.closeSubmitConfirmation();
        }
    }

    protected async confirmSubmission(): Promise<void> {
        if (await this.store.submit()) this.closeSubmitConfirmation();
    }

    @HostListener('window:beforeunload', ['$event'])
    protected beforeUnload(event: BeforeUnloadEvent): void {
        if (this.store.attemptId() && !this.store.result()) event.preventDefault();
    }

    @HostListener('window:keydown', ['$event'])
    protected navigateWithArrowKeys(event: KeyboardEvent): void {
        const target = event.target;

        if (
            this.submitDialogOpen() ||
            this.leaveDialogOpen() ||
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
