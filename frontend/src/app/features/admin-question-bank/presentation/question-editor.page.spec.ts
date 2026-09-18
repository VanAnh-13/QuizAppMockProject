import {ComponentFixture, TestBed} from '@angular/core/testing';
import {ActivatedRoute, convertToParamMap, provideRouter} from '@angular/router';
import {QUESTION_BANK_API} from '../application/question-bank-api';
import {QuestionEditorPage} from './question-editor.page';
import {QuestionEditorStore} from './question-editor.store';

describe('QuestionEditorPage', () => {
    async function render(
        setup?: (fixture: ComponentFixture<QuestionEditorPage>) => void,
    ): Promise<ComponentFixture<QuestionEditorPage>> {
        await TestBed.configureTestingModule({
            imports: [QuestionEditorPage],
            providers: [provideRouter([])],
        })
            .overrideComponent(QuestionEditorPage, {
                set: {
                    providers: [
                        QuestionEditorStore,
                        {
                            provide: ActivatedRoute,
                            useValue: {snapshot: {paramMap: convertToParamMap({})}},
                        },
                        {provide: QUESTION_BANK_API, useValue: {create: vi.fn(), get: vi.fn(), update: vi.fn()}},
                    ],
                },
            })
            .compileComponents();

        const fixture = TestBed.createComponent(QuestionEditorPage);
        setup?.(fixture);
        fixture.detectChanges();
        return fixture;
    }

    it('renders the create form and preview heading', async () => {
        const fixture = await render();
        const element = fixture.nativeElement as HTMLElement;

        expect(element.querySelector('h1')?.textContent).toContain('Create question');
        expect(element.querySelector('#question-content')).not.toBeNull();
        expect(element.textContent).toContain('Learner preview');
    });

    it('binds the preview image to the typed illustration URL', async () => {
        const fixture = await render((page) => {
            page.componentInstance['store'].form.controls.image.setValue(
                'https://images.example.com/oop.png',
            );
        });
        const image = fixture.nativeElement.querySelector('img') as HTMLImageElement | null;

        expect(image?.getAttribute('src')).toBe('https://images.example.com/oop.png');
    });

    it('keeps native select labels and updates the question type through its form binding', async () => {
        const fixture = await render();
        const element = fixture.nativeElement as HTMLElement;
        const select = element.querySelector<HTMLSelectElement>('#question-type')!;
        expect(element.querySelector('label[for="question-type"]')?.textContent).toContain('Question type');
        expect(Array.from(select.options).map((option) => option.textContent?.trim())).toContain('Short answer');
        const option = Array.from(select.options).find((item) => item.textContent?.trim() === 'Short answer')!;
        select.value = option.value;
        select.dispatchEvent(new Event('change'));
        fixture.detectChanges();
        expect(fixture.componentInstance['store'].typeLabel(
            fixture.componentInstance['store'].form.controls.questionType.value,
        )).toBe('Short answer');
    });
});
