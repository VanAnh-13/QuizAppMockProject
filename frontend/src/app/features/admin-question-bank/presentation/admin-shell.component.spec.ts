import {signal} from '@angular/core';
import {TestBed} from '@angular/core/testing';
import {provideRouter, Router} from '@angular/router';
import {AuthSession} from '../../../core/auth/auth-session';
import {ConfirmationService} from '../../../shared/ui/confirmation/confirmation.service';
import {MockConfirmationService} from '../../../../testing/mock-confirmation';
import {AdminShellComponent, initialsFrom} from './admin-shell.component';

describe('AdminShellComponent', () => {
    function setup(user = {id: '1', username: 'admin', fullName: 'Admin User'}) {
        const clear = vi.fn();
        const confirmation = new MockConfirmationService();
        TestBed.configureTestingModule({
            imports: [AdminShellComponent],
            providers: [
                provideRouter([]),
                {provide: AuthSession, useValue: {user: signal(user), clear}},
                {provide: ConfirmationService, useValue: confirmation},
            ],
        });
        const router = TestBed.inject(Router);
        const navigateSpy = vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
        const fixture = TestBed.createComponent(AdminShellComponent);
        fixture.detectChanges();

        return {fixture, clear, confirmation, navigateSpy};
    }

    it('renders navigation links and user initials', () => {
        const {fixture} = setup();
        const element = fixture.nativeElement as HTMLElement;

        expect(element.querySelector('.admin-nav__avatar')?.textContent?.trim()).toBe('AU');
        expect(element.querySelector('.admin-nav__links a[href="/admin/quizzes"]')).toBeTruthy();
        expect(element.querySelector('.admin-nav__links a[href="/admin/questions"]')).toBeTruthy();
        expect(element.querySelector('.admin-nav__links a[href="/admin/users"]')).toBeTruthy();
        expect(element.querySelector('.admin-nav__links a[href="/admin/roles"]')).toBeTruthy();
    });

    it('prompts confirmation when clicking log out and clears session on accept', async () => {
        const {fixture, clear, confirmation, navigateSpy} = setup();
        const element = fixture.nativeElement as HTMLElement;
        const logoutBtn = element.querySelector<HTMLButtonElement>('.admin-nav__account button');
        expect(logoutBtn).toBeTruthy();

        logoutBtn!.click();
        await fixture.whenStable();

        expect(confirmation.lastOptions?.title).toBe('Are you sure to log out?');
        expect(confirmation.lastOptions?.confirmLabel).toBe('Yes');
        expect(confirmation.lastOptions?.cancelLabel).toBe('No');
        expect(clear).toHaveBeenCalledTimes(1);
        expect(navigateSpy).toHaveBeenCalledWith('/login');
    });

    it('does not log out when confirmation is cancelled', async () => {
        const {fixture, clear, confirmation, navigateSpy} = setup();
        confirmation.setAutoResponse(false);
        const element = fixture.nativeElement as HTMLElement;
        const logoutBtn = element.querySelector<HTMLButtonElement>('.admin-nav__account button');

        logoutBtn!.click();
        await fixture.whenStable();

        expect(clear).not.toHaveBeenCalled();
        expect(navigateSpy).not.toHaveBeenCalled();
    });

    describe('initialsFrom', () => {
        it('computes initials correctly from fullName and username', () => {
            expect(initialsFrom('Admin User', null)).toBe('AU');
            expect(initialsFrom('Admin', null)).toBe('AD');
            expect(initialsFrom(null, 'john_doe')).toBe('JO');
            expect(initialsFrom(null, null)).toBe('QT');
        });
    });
});
