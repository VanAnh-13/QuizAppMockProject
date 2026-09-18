import {computed, inject, Injectable, signal} from '@angular/core';
import {FormArray, FormControl, FormGroup, Validators} from '@angular/forms';
import {ActivatedRoute, Router} from '@angular/router';
import {apiErrorMessage} from '../../../core/api/api-error';
import {QUESTION_BANK_API} from '../application/question-bank-api';
import {AdminQuestionAnswerDraft, QuestionLevel, QuestionType, questionTypeLabel,} from '../domain/admin-question';
import {ConfirmationService} from '../../../shared/ui/confirmation/confirmation.service';

export interface AnswerFormValue {
    text: string;
    isCorrect: boolean;
    isActive: boolean;
}

type AnswerFormGroup = FormGroup<{
    text: FormControl<string>;
    isCorrect: FormControl<boolean>;
    isActive: FormControl<boolean>;
}>;

@Injectable()
export class QuestionEditorStore {
    readonly types = [
        QuestionType.SingleChoice,
        QuestionType.MultipleChoice,
        QuestionType.TrueFalse,
        QuestionType.FillInTheBlanks,
        QuestionType.ShortAnswer,
        QuestionType.LongAnswer,
    ] as const;
    readonly levels = [QuestionLevel.Easy, QuestionLevel.Medium, QuestionLevel.Hard] as const;
    readonly typeLabel = questionTypeLabel;
    readonly form = new FormGroup({
        content: new FormControl('', {nonNullable: true, validators: [Validators.required]}),
        image: new FormControl('', {nonNullable: true}),
        level: new FormControl<QuestionLevel>(QuestionLevel.Medium, {nonNullable: true}),
        questionType: new FormControl<QuestionType>(QuestionType.MultipleChoice, {nonNullable: true}),
        isActive: new FormControl(true, {nonNullable: true}),
        answers: new FormArray<AnswerFormGroup>([]),
    });
    readonly isLoading = signal(false);
    readonly isSaving = signal(false);
    readonly errorMessage = signal<string | null>(null);
    readonly questionId = signal<string | null>(null);
    readonly isNew = computed(() => this.questionId() === null);
    readonly usesChoiceAnswers = computed(() => usesChoiceAnswers(this.form.controls.questionType.value));
    readonly usesAcceptedAnswers = computed(() => usesAcceptedAnswers(this.form.controls.questionType.value));
    readonly previewAnswers = signal<readonly AdminQuestionAnswerDraft[]>([]);
    private readonly api = inject(QUESTION_BANK_API);
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly confirmation = inject(ConfirmationService);

    constructor() {
        const rawId = this.route.snapshot.paramMap.get('questionId');
        this.questionId.set(rawId && rawId !== 'new' ? rawId : null);
        this.form.controls.questionType.valueChanges.subscribe((type) =>
            this.resetAnswersFor(type, !this.isNew()),
        );
        this.form.controls.answers.valueChanges.subscribe(() =>
            this.previewAnswers.set(this.form.getRawValue().answers.map(toDraft)),
        );
        this.resetAnswersFor(this.form.controls.questionType.value, !this.isNew());
        if (!this.isNew()) void this.load(this.questionId()!);
    }

    addAnswer(seed?: Partial<AnswerFormValue>, required = this.isNew()): void {
        this.form.controls.answers.push(answerGroup(seed, required));
    }

    async removeAnswer(index: number): Promise<void> {
        const answer = this.form.controls.answers.at(index)?.value;
        if (answer?.text?.trim()) {
            const confirmed = await this.confirmation.confirm({
                title: 'Are you sure to delete this answer option?',
                message: `Delete answer "${answer.text.trim()}"?`,
                confirmLabel: 'Yes',
                cancelLabel: 'No',
                variant: 'danger',
                icon: 'delete',
            });
            if (!confirmed) return;
        }
        this.form.controls.answers.removeAt(index);
    }

    async cancel(): Promise<void> {
        if (this.form.dirty) {
            const confirmed = await this.confirmation.confirm({
                title: 'Are you sure to discard your changes?',
                message: 'Unsaved question details will be lost.',
                confirmLabel: 'Yes',
                cancelLabel: 'No',
                variant: 'warning',
                icon: 'warning',
            });
            if (!confirmed) return;
        }
        void this.router.navigateByUrl('/admin/questions');
    }

    async save(): Promise<void> {
        this.form.markAllAsTouched();
        const validation = validateDraft(
            this.form.getRawValue(),
            this.isNew(),
            this.form.controls.questionType.value,
        );
        if (!this.form.valid || validation) {
            this.errorMessage.set(validation ?? 'Question details are incomplete.');
            return;
        }

        const value = this.form.getRawValue();
        const draft = {
            content: value.content.trim(),
            image: value.image.trim() || null,
            level: value.level,
            questionType: value.questionType,
            isActive: value.isActive,
            answers: value.answers.map(toDraft),
        };

        const isEditing = !this.isNew();
        const actionTitle = isEditing ? 'Are you sure to save changes to this question?' : 'Are you sure to create this question?';
        const confirmed = await this.confirmation.confirm({
            title: actionTitle,
            message: `Save question "${draft.content.slice(0, 50)}..."?`,
            confirmLabel: 'Yes',
            cancelLabel: 'No',
            variant: 'primary',
            icon: 'help_outline',
        });
        if (!confirmed) return;

        this.isSaving.set(true);
        this.errorMessage.set(null);
        try {
            if (this.isNew()) await this.api.create(draft);
            else await this.api.update(this.questionId()!, draft);
            await this.confirmation.notify({
                title: 'Question saved',
                message: 'The question has been saved successfully.',
                variant: 'success',
                icon: 'check_circle',
            });
            await this.router.navigateByUrl('/admin/questions');
        } catch (error) {
            this.errorMessage.set(apiErrorMessage(error, 'Could not save the question. Try again.'));
        } finally {
            this.isSaving.set(false);
        }
    }

    private async load(id: string): Promise<void> {
        this.isLoading.set(true);
        this.errorMessage.set(null);
        try {
            const question = await this.api.get(id);
            this.form.patchValue({
                content: question.content,
                image: question.image ?? '',
                level: question.level ?? QuestionLevel.Medium,
                questionType: question.questionType,
                isActive: question.isActive,
            });
            this.resetAnswersFor(question.questionType, true, false);
        } catch (error) {
            this.errorMessage.set(apiErrorMessage(error, 'Could not load the question. Try again.'));
        } finally {
            this.isLoading.set(false);
        }
    }

    private resetAnswersFor(type: QuestionType, optionalAnswers = false, seed = true): void {
        this.form.controls.answers.clear();
        if (!seed) return;
        const required = !optionalAnswers;
        if (type === QuestionType.TrueFalse) {
            this.addAnswer({text: 'True', isCorrect: true}, required);
            this.addAnswer({text: 'False', isCorrect: false}, required);
            return;
        }
        if (usesChoiceAnswers(type) || usesAcceptedAnswers(type)) {
            this.addAnswer({isCorrect: true}, required);
            this.addAnswer({isCorrect: type !== QuestionType.FillInTheBlanks && type !== QuestionType.ShortAnswer}, required);
        }
    }
}

export function usesChoiceAnswers(type: QuestionType): boolean {
    return (
        type === QuestionType.SingleChoice ||
        type === QuestionType.MultipleChoice ||
        type === QuestionType.TrueFalse
    );
}

export function usesAcceptedAnswers(type: QuestionType): boolean {
    return type === QuestionType.FillInTheBlanks || type === QuestionType.ShortAnswer;
}

export function validateDraft(
    value: { content: string; answers: readonly AnswerFormValue[] },
    isNew: boolean,
    type: QuestionType,
): string | null {
    if (!value.content.trim()) return 'Question text is required.';
    if (!isNew) return null;

    const active = value.answers.filter((answer) => answer.isActive && answer.text.trim());
    if (type === QuestionType.TrueFalse) {
        return active.length === 2 && active.filter((answer) => answer.isCorrect).length === 1
            ? null
            : 'True / False needs exactly two options and one correct answer.';
    }
    if (type === QuestionType.SingleChoice) {
        return active.length >= 2 && active.filter((answer) => answer.isCorrect).length === 1
            ? null
            : 'Single choice needs at least two options and exactly one correct answer.';
    }
    if (type === QuestionType.MultipleChoice) {
        return active.length >= 2 && active.some((answer) => answer.isCorrect)
            ? null
            : 'Multiple choice needs at least two options and one correct answer.';
    }
    if (usesAcceptedAnswers(type)) {
        return active.length > 0 && active.every((answer) => answer.isCorrect)
            ? null
            : 'Fill-in and short-answer questions need at least one accepted answer, all marked correct.';
    }
    return null;
}

function answerGroup(seed: Partial<AnswerFormValue> = {}, required = true): AnswerFormGroup {
    return new FormGroup({
        text: new FormControl(seed.text ?? '', {
            nonNullable: true,
            validators: required ? [Validators.required] : [],
        }),
        isCorrect: new FormControl(seed.isCorrect ?? false, {nonNullable: true}),
        isActive: new FormControl(seed.isActive ?? true, {nonNullable: true}),
    });
}

function toDraft(answer: AnswerFormValue): AdminQuestionAnswerDraft {
    return {
        text: answer.text.trim(),
        isCorrect: answer.isCorrect,
        isActive: answer.isActive,
    };
}
