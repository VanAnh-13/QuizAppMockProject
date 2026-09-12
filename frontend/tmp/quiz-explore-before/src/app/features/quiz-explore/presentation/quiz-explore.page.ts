import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { SiteFooterComponent } from '../../../shared/ui/site-footer/site-footer.component';
import { SiteHeaderComponent } from '../../../shared/ui/site-header/site-header.component';
import { quizCatalogProvider } from '../data/quiz-catalog.provider';
import { QuizSummary } from '../domain/quiz-summary';
import { QuizExploreStore } from './quiz-explore.store';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, SiteFooterComponent, SiteHeaderComponent],
  providers: [quizCatalogProvider, QuizExploreStore],
  selector: 'app-quiz-explore-page',
  styleUrl: './quiz-explore.page.css',
  templateUrl: './quiz-explore.page.html',
})
export class QuizExplorePage {
  protected readonly searchControl = new FormControl('', { nonNullable: true });
  protected readonly statusLabels: Readonly<Record<QuizSummary['status'], string>> = {
    open: 'Đang mở',
  };
  protected readonly store = inject(QuizExploreStore);

  protected applySearch(): void {
    this.store.applySearch(this.searchControl.value);
  }
}
