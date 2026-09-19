import {ChangeDetectionStrategy, Component, inject, signal} from '@angular/core';
import {ReactiveFormsModule} from '@angular/forms';
import {RouterLink} from '@angular/router';
import {AdminShellComponent} from './admin-shell.component';
import {provideQuestionBank} from '../infrastructure/question-bank.provider';
import {QuestionEditorStore} from './question-editor.store';
import {questionLevelLabel, QuestionType} from '../domain/admin-question';
import {NgOptimizedImage} from '@angular/common';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [AdminShellComponent, ReactiveFormsModule, RouterLink, NgOptimizedImage],
    providers: [provideQuestionBank(), QuestionEditorStore],
    selector: 'app-question-editor-page',
    styleUrl: './question-editor.page.css',
    templateUrl: './question-editor.page.html',
})
export class QuestionEditorPage {
    protected readonly store = inject(QuestionEditorStore);
    protected readonly QuestionType = QuestionType;
    protected readonly questionLevelLabel = questionLevelLabel;
    protected readonly selectedPreviewOptionIndex = signal<number | null>(null);

    protected optionLetter(index: number): string {
        return String.fromCharCode(65 + index);
    }

    protected togglePreviewSelection(index: number): void {
        this.selectedPreviewOptionIndex.update((current) => (current === index ? null : index));
    }
}
