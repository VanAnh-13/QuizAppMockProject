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
        roles: [{id: 'role-id', roleName: 'Học viên'}],
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
});
