import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { QuizSummary } from '../../domain/quiz-summary';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-quiz-card',
  styleUrl: './quiz-card.component.css',
  templateUrl: './quiz-card.component.html',
})
export class QuizCardComponent {
  readonly quiz = input.required<QuizSummary>();
  readonly detailRequested = output<QuizSummary>();
  protected readonly statusLabels: Readonly<Record<QuizSummary['status'], string>> = {
    open: 'Đang mở',
  };
}
