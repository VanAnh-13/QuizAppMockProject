import {provideHttpClient, withInterceptors} from '@angular/common/http';
import {HttpTestingController, provideHttpClientTesting} from '@angular/common/http/testing';
import {ViewportScroller} from '@angular/common';
import {TestBed} from '@angular/core/testing';
import {provideRouter, Router} from '@angular/router';
import {RouterTestingHarness} from '@angular/router/testing';
import {routes} from '../../../app.routes';
import {authInterceptor} from '../../../core/auth/auth.interceptor';
import {AuthSession} from '../../../core/auth/auth-session';

describe('Password recovery routes', () => {
    const userId = '11111111-1111-1111-1111-111111111111';
    const token = 'A'.repeat(43);
    const resetUrl = `/reset-password#userId=${userId}&token=${token}`;

    it('keeps reset credentials out of every navigation link', async () => {
        const harness = await RouterTestingHarness.create(resetUrl);
        const links = harness.routeNativeElement!.querySelectorAll<HTMLAnchorElement>('a[href]');
        expect(links.length).toBeGreaterThan(0);
        for (const link of links) {
            expect(link.href).not.toContain(token);
            expect(link.href).not.toContain(userId);
        }
    });
    beforeEach(() => TestBed.configureTestingModule({
        providers: [provideRouter(routes), provideHttpClient(withInterceptors([authInterceptor])),
            provideHttpClientTesting(), {provide: ViewportScroller, useValue: {scrollToPosition: vi.fn()}}],
    }));
    afterEach(() => TestBed.inject(HttpTestingController).verify());

    it('resets from the fragment, clears the session and returns to login without the token', async () => {
        const session = TestBed.inject(AuthSession);
        session.set({token: 'old-session', expiresAt: '2099-01-01T00:00:00Z',
            userDto: {id: userId, username: 'learner', fullName: null}}, true);
        const harness = await RouterTestingHarness.create(resetUrl);
        const element = harness.routeNativeElement!;
        fill(element, 'newPassword', 'New-password-123!');
        fill(element, 'confirmNewPassword', 'New-password-123!');
        element.querySelector<HTMLButtonElement>('button[type="submit"]')!.click();
        const request = TestBed.inject(HttpTestingController).expectOne('/api/auth/reset-password');
        expect(request.request.headers.has('Authorization')).toBe(false);
        expect(request.request.body).toEqual({userId, token, newPassword: 'New-password-123!', confirmNewPassword: 'New-password-123!'});
        request.flush(null, {status: 204, statusText: 'No Content'});
        await harness.fixture.whenStable();
        harness.detectChanges();
        expect(TestBed.inject(Router).url).toBe('/login');
        expect(session.token()).toBeNull();
        expect(localStorage.getItem('quizapp.session')).toBeNull();
        expect(harness.routeNativeElement!.querySelector('[role="status"]')?.textContent).toContain('Your password has been reset');
    });

    it.each(['/reset-password', '/reset-password#token=bad', '/reset-password#userId=bad&token=bad'])(
        'shows an actionable invalid-link state for %s', async (url) => {
            const harness = await RouterTestingHarness.create(url);
            expect(harness.routeNativeElement!.querySelector('[role="alert"]')?.textContent).toContain('invalid or has expired');
            expect(harness.routeNativeElement!.querySelector('form')).toBeNull();
            expect(harness.routeNativeElement!.querySelector('a.primary-button')?.getAttribute('href')).toBe('/forgot-password');
        },
    );

    it('validates password confirmation before sending a request', async () => {
        const harness = await RouterTestingHarness.create(resetUrl);
        const element = harness.routeNativeElement!;
        fill(element, 'newPassword', 'New-password-123!');
        fill(element, 'confirmNewPassword', 'Another-password-123!');
        element.querySelector<HTMLButtonElement>('button[type="submit"]')!.click();
        await harness.fixture.whenStable();
        harness.detectChanges();
        expect(element.querySelector('#confirmNewPassword-error')?.textContent).toContain('Passwords do not match');
        TestBed.inject(HttpTestingController).expectNone('/api/auth/reset-password');
    });

    it.each([
        [0, 'Cannot connect to the server'],
        [429, 'Too many requests'],
        [503, 'Could not process your request'],
    ])('allows retry after a request fails with HTTP %s', async (status, message) => {
        const harness = await RouterTestingHarness.create('/forgot-password');
        const element = harness.routeNativeElement!;
        fill(element, 'email', 'learner@example.com');
        element.querySelector<HTMLButtonElement>('button[type="submit"]')!.click();
        const request = TestBed.inject(HttpTestingController).expectOne('/api/auth/forgot-password');
        if (status === 0) request.error(new ProgressEvent('error'));
        else request.flush({}, {status, statusText: 'Failure'});
        await harness.fixture.whenStable();
        harness.detectChanges();
        expect(element.querySelector('[role="alert"]')?.textContent).toContain(message);
        expect(element.querySelector<HTMLButtonElement>('button[type="submit"]')!.disabled).toBe(false);
    });

    it('offers a new link when the server rejects an expired link', async () => {
        const harness = await RouterTestingHarness.create(resetUrl);
        const element = harness.routeNativeElement!;
        fill(element, 'newPassword', 'New-password-123!');
        fill(element, 'confirmNewPassword', 'New-password-123!');
        element.querySelector<HTMLButtonElement>('button[type="submit"]')!.click();
        TestBed.inject(HttpTestingController).expectOne('/api/auth/reset-password')
            .flush({}, {status: 400, statusText: 'Bad Request'});
        await harness.fixture.whenStable();
        harness.detectChanges();
        expect(element.querySelector('[role="alert"]')?.textContent).toContain('invalid or has expired');
        expect(element.querySelector('form')).toBeNull();
    });

    it('blocks duplicate requests while the first one is pending', async () => {
        const harness = await RouterTestingHarness.create('/forgot-password');
        const element = harness.routeNativeElement!;
        fill(element, 'email', 'learner@example.com');
        const form = element.querySelector('form')!;
        form.dispatchEvent(new Event('submit', {bubbles: true, cancelable: true}));
        form.dispatchEvent(new Event('submit', {bubbles: true, cancelable: true}));
        const request = TestBed.inject(HttpTestingController).expectOne('/api/auth/forgot-password');
        request.flush({message: 'If an active account matches this email, you will receive a password reset link.'});
        await harness.fixture.whenStable();
    });

    function fill(element: HTMLElement, id: string, value: string): void {
        const input = element.querySelector<HTMLInputElement>(`#${id}`)!;
        input.value = value;
        input.dispatchEvent(new Event('input'));
    }

    it('requests a reset link and displays a generic confirmation', async () => {
        const harness = await RouterTestingHarness.create('/forgot-password');
        const element = harness.routeNativeElement!;
        fill(element, 'email', 'learner@example.com');
        element.querySelector<HTMLButtonElement>('button[type="submit"]')!.click();
        const request = TestBed.inject(HttpTestingController).expectOne('/api/auth/forgot-password');
        expect(request.request.method).toBe('POST');
        expect(request.request.body).toEqual({email: 'learner@example.com'});
        expect(request.request.headers.has('Authorization')).toBe(false);
        request.flush({message: 'If an active account matches this email, you will receive a password reset link.'},
            {status: 202, statusText: 'Accepted'});
        await harness.fixture.whenStable();
        harness.detectChanges();
        expect(element.querySelector('[role="status"]')?.textContent).toContain('If an active account');
    });
});
