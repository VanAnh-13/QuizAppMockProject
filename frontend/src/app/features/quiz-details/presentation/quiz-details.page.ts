import {ChangeDetectionStrategy, Component, inject, signal} from '@angular/core';
import {DatePipe} from '@angular/common';
import {ActivatedRoute, RouterLink} from '@angular/router';
import {ATTEMPT_API, AttemptResult} from '../../quiz-attempt/application/attempt-api';
import {attemptContentProvider} from '../../quiz-attempt/infrastructure/attempt-content.provider';
import {apiErrorMessage} from '../../../core/api/api-error';
import {AuthDialogComponent} from '../../../shared/ui/auth-dialog/auth-dialog.component';
import {QuizDetailsSnapshot} from '../application/quiz-details-content';
import {QUIZ_DETAILS_CONTENT, quizDetailsContentProvider,} from '../infrastructure/quiz-details-content.provider';

interface InfoMessage {
    readonly title: string;
    readonly message: string;
}

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [AuthDialogComponent, DatePipe, RouterLink],
    providers: [quizDetailsContentProvider, attemptContentProvider],
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

    protected readonly quizId = signal('');
    protected readonly title = signal('');
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
            void this.load();
        });
    }

    protected async load(): Promise<void> {
        const quizId = this.quizId();
        if (!quizId) {
            this.isLoading.set(false);
            this.errorMessage.set('Không tìm thấy mã quiz trên đường dẫn.');
            return;
        }
        this.isLoading.set(true);
        this.errorMessage.set(null);

        try {
            const snapshot = await this.content.load(quizId);
            this.title.set(snapshot.title);
            this.description.set(snapshot.description);
            this.categoryLabel.set(snapshot.categoryLabel);
            this.metrics.set(snapshot.metrics);
            this.topics.set(snapshot.topics);
            this.guidelines.set(snapshot.guidelines);
            this.formatFacts.set(snapshot.formatFacts);
        } catch {
            this.errorMessage.set('Không thể tải chi tiết quiz từ máy chủ. Vui lòng thử lại.');
        } finally {
            this.isLoading.set(false);
        }
    }

    protected openInfo(title: string, message: string): void {
        this.activeInfo.set({title, message});
    }

    protected closeInfo(): void {
        this.activeInfo.set(null);
    }

    protected openHistory(): void {
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
                apiErrorMessage(error, 'Không thể tải lịch sử làm bài. Vui lòng thử lại.'),
            );
        } finally {
            this.historyLoading.set(false);
        }
    }
}
