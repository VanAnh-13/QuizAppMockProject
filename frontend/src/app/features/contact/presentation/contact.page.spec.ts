import {TestBed} from '@angular/core/testing';
import {provideRouter} from '@angular/router';
import {ContactApi} from '../application/contact-api';
import {CONTACT_CONFIG} from '../application/contact-config';
import {ContactPage} from './contact.page';
import {ContactStore} from './contact.store';

describe('ContactPage', () => {
    async function render(send = vi.fn().mockResolvedValue({
        id: 'feedback-123', receivedAt: '2026-09-20T00:00:00Z', confirmationEmailSent: true,
    })) {
        await TestBed.configureTestingModule({
            imports: [ContactPage],
            providers: [provideRouter([])],
        })
            .overrideComponent(ContactPage, {
                set: {providers: [{provide: ContactApi, useValue: {send}}, ContactStore]},
            })
            .compileComponents();

        const fixture = TestBed.createComponent(ContactPage);
        fixture.detectChanges();
        return {fixture, element: fixture.nativeElement as HTMLElement, send};
    }

    it('renders the contact form, office details, and FAQ disclosures without unverified email', async () => {
        const {element} = await render();

        expect(element.querySelector('h1')?.textContent).toContain('Contact us');
        expect(element.querySelector('form#contact-form')).toBeTruthy();
        expect(element.querySelector('a[href^="mailto:"]')).toBeNull();
        expect(element.querySelectorAll('details.faq-item').length).toBe(3);
    });

    it('exposes a verified contact email when configured', async () => {
        await TestBed.configureTestingModule({
            imports: [ContactPage],
            providers: [
                provideRouter([]),
                {provide: CONTACT_CONFIG, useValue: {email: 'contact@quizapp.internal'}},
            ],
        })
            .overrideComponent(ContactPage, {
                set: {providers: [{provide: ContactApi, useValue: {send: vi.fn()}}, ContactStore]},
            })
            .compileComponents();

        const fixture = TestBed.createComponent(ContactPage);
        fixture.detectChanges();
        const element = fixture.nativeElement as HTMLElement;

        const emailLink = element.querySelector('a[href="mailto:contact@quizapp.internal"]');
        expect(emailLink).not.toBeNull();
        expect(emailLink?.textContent).toContain('contact@quizapp.internal');
    });

    it('renders decorative ambient glows hidden from assistive technology', async () => {
        const {element} = await render();
        const glows = element.querySelectorAll('.ambient-glow');
        expect(glows.length).toBe(2);
        glows.forEach(glow => {
            expect(glow.getAttribute('aria-hidden')).toBe('true');
        });
    });

    it('announces required-field errors and focuses the first invalid control after an empty submit', async () => {
        const {fixture, element, send} = await render();

        const fullName = element.querySelector<HTMLInputElement>('#fullName')!;
        const focusSpy = vi.spyOn(fullName, 'focus');

        element.querySelector<HTMLFormElement>('#contact-form')!.requestSubmit();
        await fixture.whenStable();
        fixture.detectChanges();

        expect(send).not.toHaveBeenCalled();
        expect(focusSpy).toHaveBeenCalled();
        expect(fullName.getAttribute('aria-invalid')).toBe('true');
        expect(element.querySelector('#email')?.getAttribute('aria-invalid')).toBe('true');
        expect(element.querySelector('#message')?.getAttribute('aria-invalid')).toBe('true');
        expect(element.textContent).toContain('Full name is required.');
        expect(element.querySelector('[role="status"]')).toBeNull();
    });

    it('shows a success notice after a valid submission', async () => {
        const {fixture, element, send} = await render();

        setInput(element, '#fullName', 'Nguyen Van An');
        setInput(element, '#email', 'an@example.com');
        setInput(element, '#subject', 'Quiz feedback');
        setInput(element, '#message', 'The Angular quiz helped me prepare for the exam.');
        element.querySelector<HTMLFormElement>('#contact-form')!.requestSubmit();
        await fixture.whenStable();
        fixture.detectChanges();

        expect(send).toHaveBeenCalledExactlyOnceWith({
            fullName: 'Nguyen Van An',
            email: 'an@example.com',
            subject: 'Quiz feedback',
            message: 'The Angular quiz helped me prepare for the exam.',
        });
        expect(element.querySelector('[role="status"]')?.textContent).toContain(
            'Thank you for your feedback!',
        );
        expect(element.textContent).toContain('A confirmation email has been sent');
        expect(element.textContent).toContain('feedback-123');
        expect(element.querySelector<HTMLInputElement>('#fullName')?.value).toBe('');
    });
});

function setInput(element: HTMLElement, selector: string, value: string): void {
    const input = element.querySelector<HTMLInputElement | HTMLTextAreaElement>(selector)!;
    input.value = value;
    input.dispatchEvent(new Event('input'));
}
