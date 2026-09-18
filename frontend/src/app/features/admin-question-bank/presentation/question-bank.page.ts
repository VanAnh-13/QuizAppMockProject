import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {FormControl, ReactiveFormsModule} from '@angular/forms';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {debounceTime} from 'rxjs';
import {RouterLink} from '@angular/router';
import {AdminShellComponent} from './admin-shell.component';
import {provideQuestionBank} from '../infrastructure/question-bank.provider';
import {QuestionBankStore} from './question-bank.store';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [AdminShellComponent, ReactiveFormsModule, RouterLink],
    providers: [provideQuestionBank(), QuestionBankStore],
    selector: 'app-question-bank-page',
    styleUrl: './question-bank.page.css',
    templateUrl: './question-bank.page.html',
})
export class QuestionBankPage {
    protected readonly store = inject(QuestionBankStore);
    protected readonly searchControl = new FormControl('', {nonNullable: true});

    constructor() {
        this.searchControl.valueChanges
            .pipe(debounceTime(250), takeUntilDestroyed())
            .subscribe((value) => this.store.applySearch(value));
    }
}
