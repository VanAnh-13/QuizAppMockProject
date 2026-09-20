import {Component} from '@angular/core';
import {TestBed} from '@angular/core/testing';
import {provideRouter, Router} from '@angular/router';
import {SiteFooterComponent} from './site-footer.component';

describe('SiteFooterComponent', () => {
    @Component({template: ''})
    class BlankComponent {}

    it('sets aria-current="page" on the active footer link', async () => {
        TestBed.configureTestingModule({
            imports: [SiteFooterComponent],
            providers: [
                provideRouter([
                    {path: 'about', component: BlankComponent},
                    {path: 'contact', component: BlankComponent},
                ]),
            ],
        });

        const router = TestBed.inject(Router);
        const fixture = TestBed.createComponent(SiteFooterComponent);
        fixture.detectChanges();

        await router.navigateByUrl('/about');
        fixture.detectChanges();

        const navLinks = Array.from(
            (fixture.nativeElement as HTMLElement).querySelectorAll<HTMLAnchorElement>(
                'nav[aria-label="Footer navigation"] a',
            ),
        );
        const aboutLink = navLinks.find((link) => link.getAttribute('href') === '/about');
        const contactLink = navLinks.find((link) => link.getAttribute('href') === '/contact');

        expect(aboutLink?.getAttribute('aria-current')).toBe('page');
        expect(contactLink?.getAttribute('aria-current')).toBeNull();

        await router.navigateByUrl('/contact');
        fixture.detectChanges();

        expect(aboutLink?.getAttribute('aria-current')).toBeNull();
        expect(contactLink?.getAttribute('aria-current')).toBe('page');
    });
});
