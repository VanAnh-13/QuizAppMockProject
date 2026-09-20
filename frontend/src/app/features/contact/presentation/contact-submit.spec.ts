import {provideHttpClient} from '@angular/common/http';
import {HttpTestingController, provideHttpClientTesting} from '@angular/common/http/testing';
import {TestBed} from '@angular/core/testing';
import {provideRouter} from '@angular/router';
import {ContactPage} from './contact.page';

describe('Contact form HTTP submission', () => {
    const receipt = {id: 'feedback-123', receivedAt: '2026-09-20T00:00:00Z', confirmationEmailSent: true};
    const message = {
        fullName: 'Nguyen Van An',
        email: 'an@example.com',
        subject: 'Quiz feedback',
        message: 'The Angular quiz helped me prepare for the exam.',
    };

    afterEach(() => {
        TestBed.inject(HttpTestingController).verify();
    });

    async function render() {
        await TestBed.configureTestingModule({
            imports: [ContactPage],
            providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
        }).compileComponents();
        const fixture = TestBed.createComponent(ContactPage);
        fixture.detectChanges();
        const element = fixture.nativeElement as HTMLElement;
        const form = element.querySelector<HTMLFormElement>('#contact-form')!;
        const button = form.querySelector<HTMLButtonElement>('button[type="submit"]')!;

        for (const [name, value] of Object.entries(message)) {
            const input = form.querySelector<HTMLInputElement | HTMLTextAreaElement>(`#${name}`)!;
            input.value = value;
            input.dispatchEvent(new Event('input'));
        }

        return {fixture, element, form, button, http: TestBed.inject(HttpTestingController)};
    }

    it('posts all fields through the real adapter and waits for acceptance before clearing the form', async () => {
        const {fixture, element, form, button, http} = await render();

        form.requestSubmit();
        fixture.detectChanges();
        const request = http.expectOne({method: 'POST', url: '/api/public/contact'});

        expect(request.request.body).toEqual(message);
        expect(button.disabled).toBe(true);
        expect(button.textContent).toContain('Sending…');
        expect(element.querySelector('[role="status"]')).toBeNull();
        expect(element.querySelector<HTMLTextAreaElement>('#message')?.value).toBe(message.message);

        form.requestSubmit();
        http.expectNone('/api/public/contact');

        request.flush(receipt);
        await fixture.whenStable();
        fixture.detectChanges();

        expect(button.disabled).toBe(false);
        expect(element.querySelector('[role="status"]')?.textContent).toContain('Thank you for your feedback!');
        for (const name of Object.keys(message)) {
            expect(form.querySelector<HTMLInputElement | HTMLTextAreaElement>(`#${name}`)?.value).toBe('');
        }
    });

    it.each(['server', 'network'])(
        'keeps every field after a %s failure and allows retry', async (failure) => {
            const {fixture, element, form, button, http} = await render();

            form.requestSubmit();
            const request = http.expectOne({method: 'POST', url: '/api/public/contact'});
            if (failure === 'network') {
                request.error(new ProgressEvent('error'));
            } else {
                request.flush({message: 'Email delivery unavailable.'}, {
                    status: 503,
                    statusText: 'Service Unavailable'
                });
            }
            await fixture.whenStable();
            fixture.detectChanges();

            expect(element.querySelector('[role="status"]')).toBeNull();
            expect(element.querySelector('[role="alert"]')?.textContent).toContain(
                failure === 'network' ? 'Cannot connect to the server.' : 'Could not send your message.',
            );
            expect(button.disabled).toBe(false);
            for (const [name, value] of Object.entries(message)) {
                expect(form.querySelector<HTMLInputElement | HTMLTextAreaElement>(`#${name}`)?.value).toBe(value);
            }

            form.requestSubmit();
            fixture.detectChanges();
            expect(element.querySelector('[role="alert"]')).toBeNull();
            expect(button.disabled).toBe(true);
            const retry = http.expectOne({method: 'POST', url: '/api/public/contact'});
            expect(retry.request.body).toEqual(message);
            retry.flush(receipt);
            await vi.waitFor(() => {
                fixture.detectChanges();
                expect(element.querySelector('[role="status"]')).not.toBeNull();
                expect(button.disabled).toBe(false);
            });
        },
    );

    it('shows a saved notice without claiming delivery when confirmation fails', async () => {
        const {fixture, element, form, http} = await render();
        form.requestSubmit();
        http.expectOne('/api/public/contact').flush({...receipt, confirmationEmailSent: false});
        await fixture.whenStable();
        fixture.detectChanges();

        const notice = element.querySelector('[role="status"]')?.textContent;
        expect(notice).toContain('Your message has been saved.');
        expect(notice).toContain('We could not send a confirmation email.');
        expect(notice).toContain('you do not need to submit it again.');
        expect(notice).not.toContain('A confirmation email has been sent');
        expect(element.querySelector('[role="alert"]')).toBeNull();
        expect(form.querySelector<HTMLTextAreaElement>('#message')?.value).toBe('');
    });
});
