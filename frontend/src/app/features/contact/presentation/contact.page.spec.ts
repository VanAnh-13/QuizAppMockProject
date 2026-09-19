import {TestBed} from '@angular/core/testing';
import {provideRouter} from '@angular/router';
import {ContactPage} from './contact.page';

describe('ContactPage', () => {
    async function render() {
        await TestBed.configureTestingModule({
            imports: [ContactPage],
            providers: [provideRouter([])],
        }).compileComponents();

        const fixture = TestBed.createComponent(ContactPage);
        fixture.detectChanges();
        return {fixture, element: fixture.nativeElement as HTMLElement};
    }

    it('renders the contact form, office details, and FAQ disclosures', async () => {
        const {element} = await render();

        expect(element.querySelector('h1')?.textContent).toContain('Contact us');
        expect(element.querySelector('form#contact-form')).toBeTruthy();
        expect(element.querySelector('a[href="mailto:quizapp@fpt.edu.vn"]')?.textContent).toContain(
            'quizapp@fpt.edu.vn',
        );
        expect(element.querySelectorAll('details.faq-item').length).toBe(3);
    });

    it('announces required-field errors after an empty submit', async () => {
        const {fixture, element} = await render();

        element.querySelector<HTMLFormElement>('#contact-form')!.requestSubmit();
        fixture.detectChanges();

        expect(element.querySelector('#fullName')?.getAttribute('aria-invalid')).toBe('true');
        expect(element.querySelector('#email')?.getAttribute('aria-invalid')).toBe('true');
        expect(element.querySelector('#message')?.getAttribute('aria-invalid')).toBe('true');
        expect(element.textContent).toContain('Full name is required.');
        expect(element.querySelector('[role="status"]')).toBeNull();
    });

    it('shows a success notice after a valid submission', async () => {
        const {fixture, element} = await render();
        const page = fixture.componentInstance;

        page.form.setValue({
            fullName: 'Nguyen Van An',
            email: 'an@example.com',
            subject: 'Quiz feedback',
            message: 'The Angular quiz helped me prepare for the exam.',
        });
        element.querySelector<HTMLFormElement>('#contact-form')!.requestSubmit();
        fixture.detectChanges();

        expect(element.querySelector('[role="status"]')?.textContent).toContain(
            'Thank you for your feedback!',
        );
        expect(page.form.value.fullName).toBe('');
    });
});
