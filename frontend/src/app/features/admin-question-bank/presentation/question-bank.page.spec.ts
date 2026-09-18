import {TestBed} from '@angular/core/testing';
import {provideRouter} from '@angular/router';
import {QUESTION_BANK_API} from '../application/question-bank-api';
import {QuestionLevel, QuestionType} from '../domain/admin-question';
import {QuestionBankPage} from './question-bank.page';
import {QuestionBankStore} from './question-bank.store';

describe('QuestionBankPage', () => {
    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [QuestionBankPage],
            providers: [provideRouter([])],
        })
            .overrideComponent(QuestionBankPage, {
                set: {
                    providers: [
                        QuestionBankStore,
                        {
                            provide: QUESTION_BANK_API,
                            useValue: {
                                list: vi.fn().mockResolvedValue({
                                    items: [
                                        {
                                            id: '10000000-0000-0000-0000-000000000049',
                                            content: 'Trong C#, kiểu dữ liệu nào sau đây là kiểu tham chiếu?',
                                            image: null,
                                            level: QuestionLevel.Easy,
                                            questionType: QuestionType.SingleChoice,
                                            isActive: true,
                                        },
                                    ],
                                    totalCount: 1,
                                    pageNumber: 1,
                                    pageSize: 6,
                                }),
                                setActive: vi.fn(),
                            },
                        },
                    ],
                },
            })
            .compileComponents();
    });

    it('renders the question bank from the admin API in a master-detail layout', async () => {
        const fixture = TestBed.createComponent(QuestionBankPage);
        fixture.detectChanges();
        await fixture.whenStable();
        fixture.detectChanges();
        const element = fixture.nativeElement as HTMLElement;

        expect(element.querySelector('h1')?.textContent).toContain('Question bank');
        expect(element.querySelector('.admin-card')?.textContent).toContain('kiểu tham chiếu');
        expect(element.querySelector('.admin-card')?.textContent).toContain('Single choice');
        expect(element.querySelector('[aria-current="page"]')?.textContent).toContain('Question bank');

        // Check master-detail layout
        expect(element.querySelector('.questions-directory')).toBeTruthy();
        expect(element.querySelector('.question-detail')).toBeTruthy();
        expect(element.querySelector('.question-detail h2')?.textContent).toContain('kiểu tham chiếu');
        expect(element.querySelector('.question-detail__prompt-box')?.textContent).toContain('kiểu tham chiếu');
        expect(element.querySelector('.question-detail__actions a')?.getAttribute('href')).toBe('/admin/questions/10000000-0000-0000-0000-000000000049');
    });
});
