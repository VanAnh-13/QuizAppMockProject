import {HttpErrorResponse} from '@angular/common/http';
import {TestBed} from '@angular/core/testing';
import {provideRouter} from '@angular/router';
import {AuthSession} from '../../../core/auth/auth-session';
import {ACCOUNT_API} from '../application/account-api';
import {AccountSettingsPage} from './account-settings.page';
import {AccountSettingsStore} from './account-settings.store';

describe('AccountSettingsPage', () => {
    const profile = {
        id: 'user-id',
        username: 'tuan.nguyen',
        email: 'tuan.nguyen@example.com',
        fullName: 'Nguyễn Minh Tuấn',
        phoneNumber: '0912345678',
        dateOfBirth: '1996-08-15',
        roles: [{id: 'role-id', roleName: 'Học viên', isActive: true}],
    };

    async function setup(changePassword = vi.fn().mockResolvedValue(undefined)) {
        const clear = vi.fn();
        await TestBed.configureTestingModule({
            imports: [AccountSettingsPage],
            providers: [provideRouter([]), {provide: AuthSession, useValue: {clear, user: () => null}}],
        })
            .overrideComponent(AccountSettingsPage, {
                set: {
                    providers: [
                        {
                            provide: ACCOUNT_API,
                            useValue: {profile: vi.fn().mockResolvedValue(profile), changePassword},
                        },
                        AccountSettingsStore,
                    ],
                },
            })
            .compileComponents();

        const fixture = TestBed.createComponent(AccountSettingsPage);
        fixture.detectChanges();
        await fixture.whenStable();
        fixture.detectChanges();

        return {fixture, element: fixture.nativeElement as HTMLElement, changePassword, clear};
    }

    async function submit(
        fixture: Awaited<ReturnType<typeof setup>>['fixture'],
        element: HTMLElement,
        values: Record<string, string>,
    ): Promise<void> {
        for (const [id, value] of Object.entries(values)) {
            const input = element.querySelector<HTMLInputElement>(`#${id}`)!;
            input.value = value;
            input.dispatchEvent(new Event('input'));
        }
        fixture.detectChanges();
        element.querySelector('form')!.dispatchEvent(new Event('submit', {cancelable: true}));
        fixture.detectChanges();
        await fixture.whenStable();
        fixture.detectChanges();
    }

    it('shows the profile as read-only text rather than editable inputs', async () => {
        const {element} = await setup();
        const card = element.querySelector('.profile-card')!;

        expect(card.querySelector('input')).toBeNull();
        expect(card.textContent).toContain('Nguyễn Minh Tuấn');
        expect(card.textContent).toContain('tuan.nguyen@example.com');
        expect(card.textContent).toContain('0912345678');
        expect(card.querySelector('.role-chip')?.textContent).toContain('Học viên');
        expect(card.querySelector('.profile-card__avatar')?.textContent?.trim()).toBe('MT');
    });

    it('renders only active role badges and shows an honest empty state', async () => {
        const {fixture, element} = await setup();
        const store = fixture.debugElement.injector.get(AccountSettingsStore);
        const disabled = {id: 'editor', roleName: 'Editor', isActive: false};
        store.profile.set({...profile, roles: [...profile.roles, disabled]});
        fixture.detectChanges();

        expect([...element.querySelectorAll('.role-chip')].map((badge) => badge.textContent?.trim()))
            .toEqual(['Học viên']);

        store.profile.set({...profile, roles: [disabled]});
        fixture.detectChanges();

        expect(element.querySelector('.role-chip')).toBeNull();
        expect(element.textContent).toContain('No active roles');
    });

    it('keeps the three password fields masked until the toggle is pressed', async () => {
        const {fixture, element} = await setup();

        const inputs = element.querySelectorAll<HTMLInputElement>('form input');
        expect(inputs).toHaveLength(3);
        expect([...inputs].every((input) => input.type === 'password')).toBe(true);

        const toggle = element.querySelector<HTMLButtonElement>('.password-toggle')!;
        expect(toggle.getAttribute('aria-pressed')).toBe('false');
        toggle.click();
        fixture.detectChanges();

        expect(element.querySelector<HTMLInputElement>('#currentPassword')!.type).toBe('text');
        expect(
            element.querySelector<HTMLButtonElement>('.password-toggle')!.getAttribute('aria-pressed'),
        ).toBe('true');
    });

    it('reports a mismatched confirmation without calling the API', async () => {
        const {fixture, element, changePassword} = await setup();

        await submit(fixture, element, {
            currentPassword: 'Original-password-123!',
            newPassword: 'Changed-password-456!',
            confirmNewPassword: 'Changed-password-457!',
        });

        expect(changePassword).not.toHaveBeenCalled();
        expect(element.querySelector('#confirmNewPassword-error')?.textContent).toContain(
            'Mật khẩu xác nhận chưa khớp.',
        );
        expect(
            element.querySelector('#confirmNewPassword')?.getAttribute('aria-invalid'),
        ).toBe('true');
    });

    it('ends the session and links back to sign-in after a successful change', async () => {
        const {fixture, element, changePassword, clear} = await setup();

        await submit(fixture, element, {
            currentPassword: 'Original-password-123!',
            newPassword: 'Changed-password-456!',
            confirmNewPassword: 'Changed-password-456!',
        });

        expect(changePassword).toHaveBeenCalledExactlyOnceWith({
            currentPassword: 'Original-password-123!',
            newPassword: 'Changed-password-456!',
            confirmNewPassword: 'Changed-password-456!',
        });
        expect(clear).toHaveBeenCalled();

        const success = element.querySelector('[role="status"]');
        expect(success?.textContent).toContain('Mật khẩu đã được cập nhật');
        expect(success?.querySelector('a')?.getAttribute('href')).toBe('/login');
    });

    it('explains a rejected current password and keeps the form usable', async () => {
        const {fixture, element} = await setup(
            vi.fn().mockRejectedValue(new HttpErrorResponse({status: 401})),
        );

        await submit(fixture, element, {
            currentPassword: 'Wrong-password-123!',
            newPassword: 'Changed-password-456!',
            confirmNewPassword: 'Changed-password-456!',
        });

        expect(element.querySelector('[role="alert"]')?.textContent).toContain(
            'Mật khẩu hiện tại chưa đúng',
        );
        expect(element.querySelector<HTMLInputElement>('#currentPassword')!.value).toBe('');
    });

    it.each([
        {status: 200, clearsSession: true},
        {status: 401, clearsSession: true},
        {status: 500, clearsSession: false},
    ])('handles a $status password response after the page is destroyed', async ({status, clearsSession}) => {
        let resolve!: () => void;
        let reject!: (reason: unknown) => void;
        const response = new Promise<void>((resolveResponse, rejectResponse) => {
            resolve = resolveResponse;
            reject = rejectResponse;
        });
        const {fixture, element, clear} = await setup(vi.fn().mockReturnValue(response));
        const store = fixture.debugElement.injector.get(AccountSettingsStore);
        store.form.setValue({
            currentPassword: 'Original-password-123!',
            newPassword: 'Changed-password-456!',
            confirmNewPassword: 'Changed-password-456!',
        });
        element.querySelector<HTMLButtonElement>('.password-toggle')!.click();
        const visiblePasswords = fixture.componentInstance['visiblePasswords']();
        const pending = fixture.componentInstance['submit']();

        expect(store.busy()).toBe(true);
        expect(clear).not.toHaveBeenCalled();
        fixture.destroy();
        const detectChanges = vi.spyOn(fixture.componentInstance['changeDetector'], 'detectChanges');
        const querySelector = vi.spyOn(element, 'querySelector');

        if (status === 200) resolve();
        else reject(new HttpErrorResponse({status}));

        await expect(pending).resolves.toBeUndefined();

        expect(clear).toHaveBeenCalledTimes(clearsSession ? 1 : 0);
        expect(fixture.componentInstance['visiblePasswords']()).toBe(visiblePasswords);
        expect(detectChanges).not.toHaveBeenCalled();
        expect(querySelector).not.toHaveBeenCalled();
    });
});
