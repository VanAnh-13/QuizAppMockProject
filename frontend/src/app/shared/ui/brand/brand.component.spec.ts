import {TestBed} from '@angular/core/testing';
import {provideRouter} from '@angular/router';
import {BrandComponent} from './brand.component';

describe('BrandComponent', () => {
    it('renders the brand link with the quiz material symbol icon', () => {
        TestBed.configureTestingModule({
            imports: [BrandComponent],
            providers: [provideRouter([])],
        });

        const fixture = TestBed.createComponent(BrandComponent);
        fixture.detectChanges();
        const element = fixture.nativeElement as HTMLElement;

        const link = element.querySelector('a.brand');
        expect(link).toBeTruthy();
        expect(link?.getAttribute('href')).toBe('/');

        const icon = element.querySelector('.brand__mark .material-symbols-outlined');
        expect(icon).toBeTruthy();
        expect(icon?.textContent?.trim()).toBe('quiz');

        const name = element.querySelector('.brand__name');
        expect(name?.textContent?.trim()).toBe('QuizApp');
    });
});
