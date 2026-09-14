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
        expect(element.querySelector('.quiz-card h3')?.textContent).toContain('C# Cơ bản & OOP');
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
});
