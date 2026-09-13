import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { SiteFooterComponent } from '../../../shared/ui/site-footer/site-footer.component';
import { SiteHeaderComponent } from '../../../shared/ui/site-header/site-header.component';
import { quizCatalogProvider } from '../infrastructure/quiz-catalog.provider';
import { QuizSummary } from '../domain/quiz-summary';
import { QuizExploreStore } from './quiz-explore.store';
import { LearningIllustrationComponent } from './learning-illustration/learning-illustration.component';
import { QuizCardComponent } from './quiz-card/quiz-card.component';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    SiteFooterComponent,
    SiteHeaderComponent,
    LearningIllustrationComponent,
    QuizCardComponent,
  ],
  providers: [quizCatalogProvider, QuizExploreStore],
  selector: 'app-quiz-explore-page',
  styleUrl: './quiz-explore.page.css',
  templateUrl: './quiz-explore.page.html',
})
export class QuizExplorePage {
  protected readonly searchControl = new FormControl('', { nonNullable: true });
  protected readonly store = inject(QuizExploreStore);
  private readonly router = inject(Router);

  protected applySearch(event: Event): void {
    event.preventDefault();
    this.store.applySearch(this.searchControl.value);
  }

  protected resetFilters(): void {
    this.searchControl.reset();
    this.store.resetFilters();
  }

  protected openDetails(quiz: QuizSummary): void {
    void this.router.navigate(['/quiz', quiz.id]);
  }
}
