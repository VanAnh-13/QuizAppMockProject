import {
    ChangeDetectionStrategy,
    Component,
    computed,
    ElementRef,
    HostListener,
    inject,
    signal,
    viewChild,
} from '@angular/core';
import {DatePipe, NgOptimizedImage, NgTemplateOutlet} from '@angular/common';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {ATTEMPT_API, AttemptResult} from '../../quiz-attempt/application/attempt-api';
import {apiErrorMessage} from '../../../core/api/api-error';
import {AuthSession} from '../../../core/auth/auth-session';
import {ModalDirective} from '../../../shared/ui/dialog/modal.directive';
import {ConfirmationService} from '../../../shared/ui/confirmation/confirmation.service';
import {QuizDetailsSnapshot} from '../application/quiz-details-content';
import {QUIZ_DETAILS_CONTENT, provideQuizDetails} from '../infrastructure/quiz-details-content.provider';
import {QuizDetailsResolution} from './quiz-details.resolver';

interface InfoMessage {
    readonly title: string;
    readonly message: string;
}

const INFO_MESSAGES = {
    about: {
        title: 'About QuizApp',
        message: 'More about QuizApp will be available in a future update.',
    },
    contact: {
        title: 'Contact',
        message: 'Support contact details are not available in this preview.',
    },
    notifications: {
        title: 'Notifications',
        message: 'You have no new notifications.',
    },
    help: {
        title: 'Help & FAQ',
        message: 'The help center is not available in this preview.',
    },
} as const satisfies Record<string, InfoMessage>;

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [DatePipe, RouterLink, ModalDirective, NgTemplateOutlet, NgOptimizedImage],
    providers: [provideQuizDetails()],
    selector: 'app-quiz-details-page',
    styleUrls: [
        './quiz-details.page.css',
        './quiz-details.header.css',
        './quiz-details.sidebar.css',
        './quiz-details.footer.css',
        './quiz-details.dialog.css',
    ],
    templateUrl: './quiz-details.page.html',
})
export class QuizDetailsPage {
    private readonly content = inject(QUIZ_DETAILS_CONTENT);
    private readonly attempts = inject(ATTEMPT_API);
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);

    protected readonly session = inject(AuthSession);
    private readonly confirmation = inject(ConfirmationService);
    private readonly userMenu = viewChild<ElementRef<HTMLDetailsElement>>('userMenu');

    protected readonly quizId = signal('');
    protected readonly quizUrl = computed(() => `/quiz/${encodeURIComponent(this.quizId())}`);
    protected readonly title = signal('');
    protected readonly imageUrl = signal<string | null>(null);
    protected readonly description = signal('');
    protected readonly categoryLabel = signal('C# / .NET');
    protected readonly metrics = signal<QuizDetailsSnapshot['metrics']>([]);
    protected readonly topics = signal<QuizDetailsSnapshot['topics']>([]);
    protected readonly guidelines = signal<QuizDetailsSnapshot['guidelines']>([]);
    protected readonly formatFacts = signal<QuizDetailsSnapshot['formatFacts']>([]);
    protected readonly isLoading = signal(true);
    protected readonly errorMessage = signal<string | null>(null);
    protected readonly activeInfo = signal<InfoMessage | null>(null);
    protected readonly historyDialogOpen = signal(false);
    protected readonly history = signal<readonly AttemptResult[]>([]);
    protected readonly historyLoading = signal(false);
    protected readonly historyError = signal<string | null>(null);

    constructor() {
        this.route.paramMap.subscribe((params) => {
            this.quizId.set(params.get('quizId') ?? '');
            const resolved = this.route.snapshot?.data?.['snapshot'] as QuizDetailsResolution | undefined;
            if (resolved) {
                this.applyResolution(resolved);
            } else {
                void this.load();
            }
        });
        if (this.route.data) {
            this.route.data.subscribe((data) => {
                const resolved = data['snapshot'] as QuizDetailsResolution | undefined;
                if (resolved) {
                    this.applyResolution(resolved);
                }
            });
        }
    }

    private applyResolution(resolved: QuizDetailsResolution): void {
        if ('errorMessage' in resolved) {
            this.errorMessage.set(resolved.errorMessage);
            this.isLoading.set(false);
            return;
        }
        this.applySnapshot(resolved);
    }

    private applySnapshot(snapshot: QuizDetailsSnapshot): void {
        this.title.set(snapshot.title);
        this.imageUrl.set(snapshot.imageUrl ?? null);
        this.description.set(snapshot.description);
        this.categoryLabel.set(snapshot.categoryLabel);
        this.metrics.set(snapshot.metrics);
        this.topics.set(snapshot.topics);
        this.guidelines.set(snapshot.guidelines);
        this.formatFacts.set(snapshot.formatFacts);
        this.errorMessage.set(null);
        this.isLoading.set(false);
    }

    protected async load(): Promise<void> {
        const quizId = this.quizId();
        if (!quizId) {
            this.isLoading.set(false);
            this.errorMessage.set('The URL does not include a quiz ID.');
            return;
        }
        this.isLoading.set(true);
        this.errorMessage.set(null);

        try {
            const snapshot = await this.content.load(quizId);
            this.applySnapshot(snapshot);
        } catch {
            this.errorMessage.set('Could not load quiz details from the server. Please try again.');
        } finally {
            this.isLoading.set(false);
        }
    }

    protected openInfo(id: keyof typeof INFO_MESSAGES): void {
        this.activeInfo.set(INFO_MESSAGES[id]);
    }

    protected closeInfo(): void {
        this.activeInfo.set(null);
    }

    protected async logout(): Promise<void> {
        const menu = this.userMenu()?.nativeElement;
        if (menu) {
            menu.open = false;
        }
        const confirmed = await this.confirmation.confirm({
            title: 'Are you sure to log out?',
            message: 'Are you sure you want to log out of your account?',
            confirmLabel: 'Yes',
            cancelLabel: 'No',
            variant: 'primary',
            icon: 'logout',
        });
        if (!confirmed) return;

        this.session.clear();
        this.historyDialogOpen.set(false);
        this.history.set([]);
        this.historyError.set(null);
        void this.router.navigateByUrl('/login');
    }

    @HostListener('document:click', ['$event'])
    protected onDocumentClick(event: MouseEvent): void {
        const menu = this.userMenu()?.nativeElement;
        if (menu?.open && !menu.contains(event.target as Node)) {
            menu.open = false;
        }
    }

    protected async openHistory(): Promise<void> {
        if (!this.session.token()) {
            await this.router.navigate(['/login'], {queryParams: {returnUrl: this.quizUrl()}});
            return;
        }

        this.historyDialogOpen.set(true);
        void this.loadHistory();
    }

    protected closeHistory(): void {
        this.historyDialogOpen.set(false);
    }

    protected async loadHistory(): Promise<void> {
        const quizId = this.quizId();
        if (!quizId || this.historyLoading()) return;
        this.historyLoading.set(true);
        this.historyError.set(null);
        try {
            this.history.set(await this.attempts.history(quizId));
        } catch (error) {
            this.history.set([]);
            this.historyError.set(
                apiErrorMessage(error, 'Could not load your attempt history. Please try again.'),
            );
        } finally {
            this.historyLoading.set(false);
        }
    }
}
