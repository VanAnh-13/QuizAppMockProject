import {TestBed} from '@angular/core/testing';
import {provideRouter} from '@angular/router';
import {AboutPage} from './about.page';

describe('AboutPage', () => {
    it('renders the mission, features, and guiding principles without author or tech stack', async () => {
        await TestBed.configureTestingModule({
            imports: [AboutPage],
            providers: [provideRouter([])],
        }).compileComponents();

        const fixture = TestBed.createComponent(AboutPage);
        fixture.detectChanges();
        const element = fixture.nativeElement as HTMLElement;

        expect(element.querySelector('h1')?.textContent).toContain('About QuizApp');
        expect(element.textContent).toContain('Our mission');
        expect(element.querySelectorAll('.feature-card').length).toBe(3);
        expect(element.querySelectorAll('.value-card').length).toBe(4);
        expect(element.querySelector('.values-section')?.textContent).toContain('What drives QuizApp');
        expect(element.querySelectorAll('.author-card').length).toBe(0);
        expect(element.textContent).not.toContain('Lê Văn Anh');
    });
});
