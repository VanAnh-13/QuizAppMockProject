import {TestBed} from '@angular/core/testing';
import {provideRouter, Router} from '@angular/router';
import {PrototypeQuizCatalog} from '../infrastructure/prototype-quiz-catalog';
import {QUIZ_CATALOG} from '../infrastructure/quiz-catalog.provider';
import {QuizExplorePage} from './quiz-explore.page';
import {QuizExploreStore} from './quiz-explore.store';

describe('QuizExplorePage', () => {
    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [QuizExplorePage],
            providers: [provideRouter([])],
        })
            .overrideComponent(QuizExplorePage, {
                set: {
                    providers: [{provide: QUIZ_CATALOG, useClass: PrototypeQuizCatalog}, QuizExploreStore],
                },
            })
            .compileComponents();
    });

    async function createFixture() {
        const fixture = TestBed.createComponent(QuizExplorePage);
        fixture.detectChanges();
        await fixture.whenStable();
        fixture.detectChanges();
        return fixture;
    }

    it('renders catalog data', async () => {
        const fixture = await createFixture();
        const element = fixture.nativeElement as HTMLElement;

        expect(element.querySelectorAll('.quiz-card')).toHaveLength(6);
        expect(element.querySelector('.quiz-card h3')?.textContent).toContain('C# Fundamentals & OOP');
    });

    it('routes every catalog card to its own details screen', async () => {
        const router = TestBed.inject(Router);
        const navigate = vi.spyOn(router, 'navigate').mockResolvedValue(true);
        const fixture = await createFixture();
        const element = fixture.nativeElement as HTMLElement;

        element.querySelectorAll<HTMLButtonElement>('.quiz-card__detail')[1]!.click();

        expect(navigate).toHaveBeenCalledWith(['/quiz', 'sql-server-fundamentals']);
    });

    it('links registration to the full account creation page', async () => {
        const fixture = await createFixture();
        const element = fixture.nativeElement as HTMLElement;

        expect(element.querySelector('.header__action--primary')?.getAttribute('href')).toBe('/register?returnUrl=%2F');
    });
    it('shows an empty search result and restores the catalog when filters are cleared', async () => {
        const fixture = await createFixture();
        const element = fixture.nativeElement as HTMLElement;
        const search = element.querySelector<HTMLInputElement>('#quiz-search')!;
        search.value = 'no-matching-quiz';
        search.dispatchEvent(new Event('input'));
        element.querySelector('form')!.dispatchEvent(new Event('submit', {cancelable: true}));
        fixture.detectChanges();
        await fixture.whenStable();
        fixture.detectChanges();
        expect(element.querySelector('.catalog__empty')?.textContent).toContain('No matching quizzes');
        element.querySelector<HTMLButtonElement>('.catalog__empty button')!.click();
        fixture.detectChanges();
        await fixture.whenStable();
        fixture.detectChanges();
        expect(search.value).toBe('');
        expect(element.querySelectorAll('.quiz-card')).toHaveLength(6);
    });

    it('does not render a search submit button and supports instant type-as-you-go filtering', async () => {
        const fixture = await createFixture();
        const element = fixture.nativeElement as HTMLElement;

        expect(element.querySelector('button[type="submit"]')).toBeNull();

        const search = element.querySelector<HTMLInputElement>('#quiz-search')!;
        expect(element.querySelector('.search__clear')).toBeNull();

        search.value = 'sql';
        search.dispatchEvent(new Event('input'));
        fixture.detectChanges();

        expect(element.querySelectorAll('.quiz-card')).toHaveLength(1);
        expect(element.querySelector('.quiz-card h3')?.textContent).toContain('SQL Server');

        const clearBtn = element.querySelector<HTMLButtonElement>('.search__clear');
        expect(clearBtn).not.toBeNull();
        clearBtn!.click();
        fixture.detectChanges();

        expect(search.value).toBe('');
        expect(element.querySelectorAll('.quiz-card')).toHaveLength(6);
    });
});
