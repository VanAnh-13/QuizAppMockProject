import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {ReactiveFormsModule} from '@angular/forms';
import {ActivatedRoute} from '@angular/router';
import {AdminShellComponent} from '../../admin-question-bank/presentation/admin-shell.component';
import {provideAdminQuiz} from '../infrastructure/admin-quiz.provider';
import {QuizAdminStore} from './quiz-admin.store';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [AdminShellComponent, ReactiveFormsModule],
    providers: [provideAdminQuiz(), QuizAdminStore],
    selector: 'app-quiz-admin-page',
    styleUrl: './quiz-admin.page.css',
    templateUrl: './quiz-admin.page.html',
})
export class QuizAdminPage {
    protected readonly store = inject(QuizAdminStore);

    constructor() {
        if (inject(ActivatedRoute).snapshot.url.at(-1)?.path === 'new') this.store.startCreate();
    }
}
