import {signal} from '@angular/core';
import {ComponentFixture, TestBed} from '@angular/core/testing';
import {provideRouter, Router} from '@angular/router';
import {ApiClient} from '../../../core/api/api-client';
import {AuthResponse, AuthSession} from '../../../core/auth/auth-session';
import {SiteHeaderComponent} from './site-header.component';

describe('SiteHeaderComponent', () => {
    it('renders the mobile menu for small screens', () => {
        configure(null);
        const fixture = TestBed.createComponent(SiteHeaderComponent);
        fixture.detectChanges();

        const menu = (fixture.nativeElement as HTMLElement).querySelector(
            'details.header__mobile-menu',
        );

        expect(menu).toBeTruthy();
        expect(menu?.querySelector('summary')).toBeTruthy();
    });

    it('links to registration from the mobile menu', () => {
        configure(null);
        const fixture = TestBed.createComponent(SiteHeaderComponent);
        fixture.detectChanges();

        expect(mobileAction(fixture, 'Đăng ký').getAttribute('href')).toBe('/register?returnUrl=%2F');
    });

    it('links to login from the mobile menu', () => {
        configure(null);
        const fixture = TestBed.createComponent(SiteHeaderComponent);
        fixture.detectChanges();

        expect(mobileAction(fixture, 'Đăng nhập').getAttribute('href')).toBe('/login?returnUrl=%2F');
    });

    it('clears the session, emits sessionChanged, and goes to login on mobile logout', () => {
        const {clear} = configure({id: '1', username: 'learner', fullName: 'Quiz Learner'});
        const fixture = TestBed.createComponent(SiteHeaderComponent);
        fixture.detectChanges();
        const emitted = vi.fn();
        fixture.componentInstance.sessionChanged.subscribe(emitted);

        mobileAction(fixture, 'Đăng xuất').click();

        expect(clear).toHaveBeenCalledTimes(1);
        expect(emitted).toHaveBeenCalledTimes(1);
        expect(TestBed.inject(Router).navigateByUrl).toHaveBeenCalledWith('/login');
    });

    it('renders user avatar menu and handles logout for signed in user', () => {
        const {clear} = configure({id: '1', username: 'learner', fullName: 'Quiz Learner'});
        const fixture = TestBed.createComponent(SiteHeaderComponent);
        fixture.detectChanges();
        const emitted = vi.fn();
        fixture.componentInstance.sessionChanged.subscribe(emitted);

        const avatar = (fixture.nativeElement as HTMLElement).querySelector('.header__avatar');
        expect(avatar).toBeTruthy();

        const logoutBtn = (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>(
            '[data-testid="header-logout"]',
        );
        expect(logoutBtn).toBeTruthy();
        logoutBtn!.click();

        expect(clear).toHaveBeenCalledTimes(1);
        expect(emitted).toHaveBeenCalledTimes(1);
        expect(TestBed.inject(Router).navigateByUrl).toHaveBeenCalledWith('/login');
    });
});

function configure(user: AuthResponse['userDto'] | null) {
    const clear = vi.fn();
    TestBed.configureTestingModule({
        imports: [SiteHeaderComponent],
        providers: [
            {provide: ApiClient, useValue: {post: vi.fn()}},
            {provide: AuthSession, useValue: {user: signal(user), clear}},
            provideRouter([]),
        ],
    });
    vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    return {clear};
}

function mobileAction(
    fixture: ComponentFixture<SiteHeaderComponent>,
    label: string,
): HTMLElement {
    const actions = (fixture.nativeElement as HTMLElement).querySelector('.header__mobile-actions');
    const button = Array.from(actions?.querySelectorAll('button, a') ?? []).find(
        (candidate) => candidate.textContent?.trim() === label,
    );
    expect(button, `mobile action "${label}"`).toBeTruthy();
    return button as HTMLElement;
}
