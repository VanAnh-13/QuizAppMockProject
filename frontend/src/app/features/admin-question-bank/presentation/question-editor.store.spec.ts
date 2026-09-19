import {HttpErrorResponse} from '@angular/common/http';
import {TestBed} from '@angular/core/testing';
import {ActivatedRoute, convertToParamMap, provideRouter, Router} from '@angular/router';
import {QUESTION_BANK_API} from '../application/question-bank-api';
import {QuestionLevel, QuestionType} from '../domain/admin-question';
import {QuestionEditorStore, validateDraft} from './question-editor.store';
import {ConfirmationService} from '../../../shared/ui/confirmation/confirmation.service';
import {MockConfirmationService} from '../../../../testing/mock-confirmation';

describe('QuestionEditorStore', () => {
    function setup(questionId: string | null = null) {
        const create = vi.fn().mockResolvedValue({
            id: 'created-id',
            content: 'Question hỏi mới',
            image: null,
            level: QuestionLevel.Medium,
            questionType: QuestionType.MultipleChoice,
            isActive: true,
        });
        const get = vi.fn().mockResolvedValue({
            id: 'existing-id',
            content: 'Question hỏi đã lưu',
            image: null,
            level: QuestionLevel.Easy,
            questionType: QuestionType.SingleChoice,
            isActive: true,
        });
        const update = vi.fn().mockResolvedValue(undefined);
        TestBed.configureTestingModule({
            providers: [
                provideRouter([]),
                QuestionEditorStore,
                {provide: ConfirmationService, useClass: MockConfirmationService},
                {
                    provide: ActivatedRoute,
                    useValue: {snapshot: {paramMap: convertToParamMap(questionId ? {questionId} : {})}},
                },
                {provide: QUESTION_BANK_API, useValue: {create, get, update}},
            ],
        });
        return {store: TestBed.inject(QuestionEditorStore), create, get, update, router: TestBed.inject(Router)};
    }

    it('creates a new multiple-choice question with answers', async () => {
        const {store, create, router} = setup();
        const navigate = vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
        store.form.patchValue({content: 'Những đặc điểm nào thuộc OOP?'});
        store.form.controls.answers.at(0).patchValue({text: 'Close gói', isCorrect: true});
        store.form.controls.answers.at(1).patchValue({text: 'Bytecode', isCorrect: false});

        await store.save();

        expect(create).toHaveBeenCalledWith(
            expect.objectContaining({
                content: 'Những đặc điểm nào thuộc OOP?',
                answers: [
                    {text: 'Close gói', isCorrect: true, isActive: true},
                    {text: 'Bytecode', isCorrect: false, isActive: true},
                ],
            }),
        );
        expect(navigate).toHaveBeenCalledWith('/admin/questions');
    });

    it('loads an existing question and updates stem metadata without answers', async () => {
        const {store, get, update} = setup('existing-id');
        await vi.waitUntil(() => !store.isLoading());

        expect(get).toHaveBeenCalledWith('existing-id');
        store.form.patchValue({content: 'Nội dung đã sửa'});
        await store.save();

        expect(update).toHaveBeenCalledWith(
            'existing-id',
            expect.objectContaining({content: 'Nội dung đã sửa'}),
        );
        expect(update.mock.calls[0][1].answers).toBeDefined();
    });

    it('rejects a new question without a correct choice', async () => {
        const {store, create} = setup();
        store.form.patchValue({content: 'Thiếu đáp án đúng'});
        store.form.controls.answers.at(0).patchValue({text: 'A', isCorrect: false});
        store.form.controls.answers.at(1).patchValue({text: 'B', isCorrect: false});

        await store.save();

        expect(create).not.toHaveBeenCalled();
        expect(store.errorMessage()).toContain('Multiple choice');
    });

    it('surfaces an API error when create fails', async () => {
        const {store} = setup();
        TestBed.inject(QUESTION_BANK_API).create = vi
            .fn()
            .mockRejectedValue(new HttpErrorResponse({status: 422}));
        store.form.patchValue({content: 'Question hỏi hợp lệ'});
        store.form.controls.answers.at(0).patchValue({text: 'A', isCorrect: true});
        store.form.controls.answers.at(1).patchValue({text: 'B', isCorrect: false});

        await store.save();

        expect(store.errorMessage()).toBe('Some submitted details are invalid. Please check them and try again.');
    });
});

describe('validateDraft', () => {
    it('requires one correct option for single choice', () => {
        expect(
            validateDraft(
                {
                    content: 'Question hỏi',
                    answers: [
                        {text: 'A', isCorrect: true, isActive: true},
                        {text: 'B', isCorrect: true, isActive: true},
                    ],
                },
                true,
                QuestionType.SingleChoice,
            ),
        ).toContain('Single choice');
    });
});
