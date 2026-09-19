import {ChangeDetectionStrategy, Component, input, output} from '@angular/core';
import {NgOptimizedImage} from '@angular/common';
import {QuizSummary} from '../../domain/quiz-summary';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [NgOptimizedImage],
    selector: 'app-quiz-card',
    styleUrl: './quiz-card.component.css',
    templateUrl: './quiz-card.component.html',
})
export class QuizCardComponent {
    readonly quiz = input.required<QuizSummary>();
    readonly detailRequested = output<QuizSummary>();
    protected readonly fallbackImage =
        'https://images.unsplash.com/photo-1517694712202-14dd9538aa97?q=80&w=400&auto=format&fit=crop';
    protected readonly statusLabels: Readonly<Record<QuizSummary['status'], string>> = {
        open: 'Open',
    };
}
