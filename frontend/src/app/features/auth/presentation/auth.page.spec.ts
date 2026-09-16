import {signal} from '@angular/core';
import {TestBed} from '@angular/core/testing';
import {ViewportScroller} from '@angular/common';
import {provideRouter, Router} from '@angular/router';
import {RouterTestingHarness} from '@angular/router/testing';
import {routes} from '../../../app.routes';
import {AuthSession} from '../../../core/auth/auth-session';
import {AuthApi} from '../infrastructure/auth-api';
import {AuthPage} from './auth.page';

describe('AuthPage routes', () => {
    async function setup(url: string) {
        const api = {register: vi.fn().mockResolvedValue(undefined), login: vi.fn().mockResolvedValue({})};
        const session = {set: vi.fn(), user: signal(null), token: vi.fn().mockReturnValue(null), clear: vi.fn()};
        const scrollToPosition = vi.fn();
        TestBed.configureTestingModule({
            providers: [provideRouter(routes), {
                provide: AuthApi,
                useValue: api
            }, {provide: AuthSession, useValue: session}, {provide: ViewportScroller, useValue: {scrollToPosition}}]
        });
        const harness = await RouterTestingHarness.create();
        await harness.navigateByUrl(url, AuthPage);
        const element = harness.routeNativeElement!;
        return {harness, element, api, session, scrollToPosition, router: TestBed.inject(Router)};
    }

    it('renders an initially clean login form and toggles password visibility accessibly', async () => {
        const {element, harness, scrollToPosition} = await setup('/login');
        expect(scrollToPosition).toHaveBeenCalledWith([0, 0]);
        expect(element.querySelectorAll('h1')).toHaveLength(1);
        expect(element.querySelector('h1')?.textContent).toBe('Đăng nhập');
        expect(element.querySelector('[aria-invalid="true"]')).toBeNull();
        expect(element.querySelector('input[type="email"]')).toBeNull();
        const toggle = element.querySelector<HTMLButtonElement>('[aria-controls="password"]')!;
        toggle.click();
        harness.detectChanges();
        expect(element.querySelector<HTMLInputElement>('#password')!.type).toBe('text');
        expect(toggle.getAttribute('aria-pressed')).toBe('true');
        toggle.click();
        harness.detectChanges();
        expect(element.querySelector<HTMLInputElement>('#password')!.type).toBe('password');
        expect(element.querySelector('.guest-button')?.getAttribute('href')).toBe('/');
    });

    it('shows associated field errors when an empty registration is submitted', async () => {
        const {element, harness, api} = await setup('/register');
        expect(element.querySelector('h1')?.textContent).toContain('Tạo tài khoản QuizApp');
        element.querySelector<HTMLButtonElement>('button[type="submit"]')!.click();
        await harness.fixture.whenStable();
        harness.detectChanges();
        expect(element.querySelector('#email')?.getAttribute('aria-describedby')).toBe('email-error');
        expect(element.querySelector('#email-error')?.textContent).toContain('Vui lòng điền');
        expect(api.register).not.toHaveBeenCalled();
    });

    it('creates an account, then shows login with a success notice and keeps the destination', async () => {
        const {element, harness, api, router} = await setup('/register?returnUrl=%2Fquiz%2Fabc');
        expect(element.querySelector('.header__action--text')?.getAttribute('href')).toBe('/login?returnUrl=%2Fquiz%2Fabc');
        const values = {
            familyName: 'Nguyễn Văn',
            givenName: 'An',
            email: 'an@example.com',
            username: 'an_nguyen',
            password: 'Password-123!',
            confirmPassword: 'Password-123!'
        };
        for (const [name, value] of Object.entries(values)) {
            fill(element, name, value);
        }
        element.querySelector<HTMLInputElement>('#terms')!.click();
        element.querySelector<HTMLButtonElement>('button[type="submit"]')!.click();
        await harness.fixture.whenStable();
        harness.detectChanges();
        expect(api.register).toHaveBeenCalledOnce();
        expect(router.url).toBe('/login?returnUrl=%2Fquiz%2Fabc');
        expect(harness.routeNativeElement!.querySelector('[role="status"]')?.textContent).toContain('Tài khoản đã được tạo');
        expect(harness.routeNativeElement!.querySelector<HTMLInputElement>('#password')!.value).toBe('');
    });

    it.each(['/quiz/abc', '/quiz/abc/attempt?attemptId=attempt-123'])('returns to %s after successful login', async (returnUrl) => {
        const {element, harness, session, router} = await setup(`/login?returnUrl=${encodeURIComponent(returnUrl)}`);
        const navigate = vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
        fill(element, 'username', 'learner');
        fill(element, 'password', 'Password-123!');
        element.querySelector<HTMLButtonElement>('button[type="submit"]')!.click();
        await harness.fixture.whenStable();
        expect(session.set).toHaveBeenCalledOnce();
        expect(navigate).toHaveBeenCalledWith(returnUrl);
    });

    it('redirects an anonymous attempt visit to login before creating the attempt page', async () => {
        const returnUrl = '/quiz/abc/attempt?attemptId=attempt-123';
        const {element, router} = await setup(returnUrl);

        expect(router.url).toBe(`/login?returnUrl=${encodeURIComponent(returnUrl)}`);
        expect(element.querySelector('#username')).not.toBeNull();
        expect(element.querySelector('.exam-header')).toBeNull();
    });

    it('does not redirect when the user navigates away before login resolves', async () => {
        let resolveLogin!: (value: object) => void;
        const slowLogin = new Promise<object>((resolve) => {
            resolveLogin = resolve;
        });
        const api = {register: vi.fn(), login: vi.fn().mockReturnValue(slowLogin)};
        const session = {set: vi.fn(), user: signal(null), token: vi.fn().mockReturnValue(null), clear: vi.fn()};
        TestBed.configureTestingModule({
            providers: [provideRouter(routes), {
                provide: AuthApi,
                useValue: api
            }, {provide: AuthSession, useValue: session}, {
                provide: ViewportScroller,
                useValue: {scrollToPosition: vi.fn()}
            }]
        });
        const harness = await RouterTestingHarness.create();
        await harness.navigateByUrl('/login?returnUrl=%2Fquiz%2Fabc', AuthPage);
        const element = harness.routeNativeElement!;
        fill(element, 'username', 'learner');
        fill(element, 'password', 'Password-123!');
        element.querySelector<HTMLButtonElement>('button[type="submit"]')!.click();
        expect(api.login).toHaveBeenCalledOnce();

        const router = TestBed.inject(Router);
        await harness.navigateByUrl('/');
        const navigate = vi.spyOn(router, 'navigateByUrl');

        resolveLogin({
            token: 't',
            expiresAt: '2099-01-01T00:00:00Z',
            userDto: {id: '1', username: 'u', fullName: null}
        });
        await harness.fixture.whenStable();

        expect(navigate).not.toHaveBeenCalled();
    });
});

function fill(element: HTMLElement, name: string, value: string): void {
    const input = element.querySelector<HTMLInputElement>(`#${name}`)!;
    input.value = value;
    input.dispatchEvent(new Event('input', {bubbles: true}));
}
