import {signal} from '@angular/core';
import {ComponentFixture, TestBed} from '@angular/core/testing';
import {provideRouter, Router} from '@angular/router';
import {ApiClient} from '../../../core/api/api-client';
import {AuthResponse, AuthSession} from '../../../core/auth/auth-session';
import {SiteHeaderComponent} from './site-header.component';

import {ConfirmationService} from '../confirmation/confirmation.service';
import {MockConfirmationService} from '../../../../testing/mock-confirmation';

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

        expect(mobileAction(fixture, 'Sign up').getAttribute('href')).toBe('/register?returnUrl=%2F');
    });

    it('links to login from the mobile menu', () => {
        configure(null);
        const fixture = TestBed.createComponent(SiteHeaderComponent);
        fixture.detectChanges();

        expect(mobileAction(fixture, 'Log in').getAttribute('href')).toBe('/login?returnUrl=%2F');
    });

    it('clears the session and emits sessionChanged on mobile logout when confirmed', async () => {
        const {clear} = configure({id: '1', username: 'learner', fullName: 'Quiz Learner'});
        const fixture = TestBed.createComponent(SiteHeaderComponent);
        fixture.detectChanges();
        const emitted = vi.fn();
        fixture.componentInstance.sessionChanged.subscribe(emitted);

        mobileAction(fixture, 'Log out').click();
        await fixture.whenStable();

        expect(clear).toHaveBeenCalledTimes(1);
        expect(emitted).toHaveBeenCalledTimes(1);
    });

    it('renders user avatar menu and handles logout for signed in user when confirmed', async () => {
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
        await fixture.whenStable();

        expect(clear).toHaveBeenCalledTimes(1);
        expect(emitted).toHaveBeenCalledTimes(1);
    });

    it('does not log out when user cancels confirmation', async () => {
        const {clear, confirmation} = configure({id: '1', username: 'learner', fullName: 'Quiz Learner'});
        confirmation.setAutoResponse(false);
        const fixture = TestBed.createComponent(SiteHeaderComponent);
        fixture.detectChanges();
        const emitted = vi.fn();
        fixture.componentInstance.sessionChanged.subscribe(emitted);

        const logoutBtn = (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>(
            '[data-testid="header-logout"]',
        );
        logoutBtn!.click();
        await fixture.whenStable();

        expect(clear).not.toHaveBeenCalled();
        expect(emitted).not.toHaveBeenCalled();
    });
});

function configure(user: AuthResponse['userDto'] | null) {
    const clear = vi.fn();
    const confirmation = new MockConfirmationService();
    TestBed.configureTestingModule({
        imports: [SiteHeaderComponent],
        providers: [
            {provide: ApiClient, useValue: {post: vi.fn()}},
            {provide: AuthSession, useValue: {user: signal(user), clear}},
            {provide: ConfirmationService, useValue: confirmation},
            provideRouter([]),
        ],
    });
    vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    return {clear, confirmation};
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
