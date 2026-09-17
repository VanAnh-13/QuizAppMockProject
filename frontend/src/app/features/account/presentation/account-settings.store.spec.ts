import {HttpErrorResponse} from '@angular/common/http';
import {TestBed} from '@angular/core/testing';
import {ACCOUNT_API} from '../application/account-api';
import {AccountProfile} from '../domain/account-contracts';
import {AccountSettingsStore} from './account-settings.store';

const profile: AccountProfile = {
    id: 'user-id',
    username: 'tuan.nguyen',
    email: 'tuan.nguyen@example.com',
    fullName: 'Nguyễn Minh Tuấn',
    phoneNumber: '0912345678',
    dateOfBirth: '1996-08-15',
    roles: [{id: 'role-id', roleName: 'Admin'}],
};

describe('AccountSettingsStore', () => {
    function setup(changePassword = vi.fn().mockResolvedValue(undefined)) {
        const api = {profile: vi.fn().mockResolvedValue(profile), changePassword};
        TestBed.configureTestingModule({
            providers: [AccountSettingsStore, {provide: ACCOUNT_API, useValue: api}],
        });

        return {store: TestBed.inject(AccountSettingsStore), api};
    }

    function fill(store: AccountSettingsStore, current: string, next: string, confirm = next): void {
        store.form.patchValue({
            currentPassword: current,
            newPassword: next,
            confirmNewPassword: confirm,
        });
    }

    it('loads the read-only profile and derives the avatar initials', async () => {
        const {store, api} = setup();
        await store.load();

        expect(api.profile).toHaveBeenCalled();
        expect(store.profile()).toEqual(profile);
        expect(store.initials()).toBe('MT');
        expect(store.isLoading()).toBe(false);
        expect(store.loadError()).toBeNull();
    });

    it('reports a friendly message when the profile cannot be loaded', async () => {
        const {store} = setup();
        TestBed.inject(ACCOUNT_API).profile = vi
            .fn()
            .mockRejectedValue(new HttpErrorResponse({status: 500}));

        await store.load();

        expect(store.profile()).toBeNull();
        expect(store.loadError()).toBe('Không thể tải thông tin hồ sơ. Vui lòng thử lại.');
    });

    it('sends the change to the API and requires a fresh sign-in on success', async () => {
        const {store, api} = setup();
        fill(store, 'Original-password-123!', 'Changed-password-456!');

        expect(await store.submit()).toBe(true);
        expect(api.changePassword).toHaveBeenCalledExactlyOnceWith({
            currentPassword: 'Original-password-123!',
            newPassword: 'Changed-password-456!',
            confirmNewPassword: 'Changed-password-456!',
        });
        expect(store.succeeded()).toBe(true);
        expect(store.requiresSignIn()).toBe(true);
        expect(store.error()).toBeNull();
    });

    it('always clears the password fields after a submission', async () => {
        const {store} = setup();
        fill(store, 'Original-password-123!', 'Changed-password-456!');

        await store.submit();

        expect(store.form.controls.currentPassword.value).toBe('');
        expect(store.form.controls.newPassword.value).toBe('');
        expect(store.form.controls.confirmNewPassword.value).toBe('');
    });

    it('rejects a mismatched confirmation before calling the API', async () => {
        const {store, api} = setup();
        fill(store, 'Original-password-123!', 'Changed-password-456!', 'Changed-password-457!');

        expect(await store.submit()).toBe(false);
        expect(api.changePassword).not.toHaveBeenCalled();
        expect(store.fieldError('confirmNewPassword')).toBe('Mật khẩu xác nhận chưa khớp.');
    });

    it('rejects reusing the current password', async () => {
        const {store, api} = setup();
        fill(store, 'Original-password-123!', 'Original-password-123!');

        expect(await store.submit()).toBe(false);
        expect(api.changePassword).not.toHaveBeenCalled();
        expect(store.fieldError('newPassword')).toBe('Mật khẩu mới phải khác mật khẩu hiện tại.');
    });

    it('rejects a new password shorter than the minimum length', async () => {
        const {store, api} = setup();
        fill(store, 'Original-password-123!', 'short12');

        expect(await store.submit()).toBe(false);
        expect(api.changePassword).not.toHaveBeenCalled();
        expect(store.fieldError('newPassword')).toBe('Mật khẩu cần ít nhất 8 ký tự.');
    });

    it('reports missing required fields', async () => {
        const {store, api} = setup();

        expect(await store.submit()).toBe(false);
        expect(api.changePassword).not.toHaveBeenCalled();
        expect(store.fieldError('currentPassword')).toBe('Vui lòng điền thông tin này.');
    });

    it('explains that a 401 ends the session', async () => {
        const {store} = setup(vi.fn().mockRejectedValue(new HttpErrorResponse({status: 401})));
        fill(store, 'Wrong-password-123!', 'Changed-password-456!');

        expect(await store.submit()).toBe(false);
        expect(store.succeeded()).toBe(false);
        expect(store.requiresSignIn()).toBe(true);
        expect(store.error()).toContain('Mật khẩu hiện tại chưa đúng');
    });

    it('keeps the session for a server error', async () => {
        const {store} = setup(vi.fn().mockRejectedValue(new HttpErrorResponse({status: 500})));
        fill(store, 'Original-password-123!', 'Changed-password-456!');

        expect(await store.submit()).toBe(false);
        expect(store.requiresSignIn()).toBe(false);
        expect(store.error()).toBe(
            'Không thể đổi mật khẩu. Vui lòng kiểm tra thông tin và thử lại.',
        );
    });
});
