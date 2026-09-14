import {HttpErrorResponse} from '@angular/common/http';
import {TestBed} from '@angular/core/testing';
import {AuthSession} from '../../../core/auth/auth-session';
import {AUTH_LIMITS, authReturnUrl} from '../domain/auth-contracts';
import {AuthApi} from '../infrastructure/auth-api';
import {AuthFormStore} from './auth-form.store';

describe('AuthFormStore', () => {
    function setup(registering = true) {
        const response = {token: 'test-token'};
        const api = {login: vi.fn().mockResolvedValue(response), register: vi.fn().mockResolvedValue(undefined)};
        const set = vi.fn();
        TestBed.configureTestingModule({
            providers: [
                AuthFormStore,
                {provide: AuthApi, useValue: api},
                {provide: AuthSession, useValue: {set}},
            ]
        });
        const store = TestBed.inject(AuthFormStore);
        store.configure(registering);
        store.form.patchValue({
            familyName: ' Nguyễn Văn ', givenName: ' An ', email: 'an@example.com',
            username: ' an_nguyen ', password: 'Password-123!', confirmPassword: 'Password-123!', terms: true,
        });
        return {store, api, set, response};
    }

    it('maps Vietnamese names and optional profile fields to the registration contract', async () => {
        const {store, api, set} = setup();
        store.form.patchValue({phoneNumber: ' 0912345678 ', dateOfBirth: '2000-01-02'});
        expect(await store.submit()).toBe(true);
        expect(api.register).toHaveBeenCalledExactlyOnceWith({
            username: 'an_nguyen', email: 'an@example.com', password: 'Password-123!', confirmPassword: 'Password-123!',
            profile: {fullName: 'Nguyễn Văn An', phoneNumber: '0912345678', dateOfBirth: '2000-01-02'},
        });
        expect(set).not.toHaveBeenCalled();
        expect(store.form.controls.password.value).toBe('');
    });

    it('sends null for omitted optional profile fields', async () => {
        const {store, api} = setup();
        await store.submit();
        expect(api.register.mock.calls[0][0].profile).toEqual({
            fullName: 'Nguyễn Văn An',
            phoneNumber: null,
            dateOfBirth: null
        });
    });

    it.each([
        {givenName: '  '}, {email: 'invalid-email'}, {password: 'short', confirmPassword: 'short'},
        {confirmPassword: 'different'}, {terms: false}, {dateOfBirth: '2999-01-01'},
        {familyName: 'n'.repeat(AUTH_LIMITS.fullName)},
    ])('blocks invalid registration before sending a request: %j', async (patch) => {
        const {store, api} = setup();
        store.form.patchValue(patch);
        expect(await store.submit()).toBe(false);
        expect(api.register).not.toHaveBeenCalled();
    });

    it('keeps profile data and gives a useful conflict error for retry', async () => {
        const {store, api} = setup();
        api.register.mockRejectedValueOnce(new HttpErrorResponse({status: 409}));
        expect(await store.submit()).toBe(false);
        expect(store.error()).toContain('Tên đăng nhập hoặc email đã được sử dụng');
        expect(store.form.controls.email.value).toBe('an@example.com');
        expect(store.form.controls.password.value).toBe('');
        expect(store.busy()).toBe(false);
    });

    it('logs in with only username and password and forwards the remember preference', async () => {
        const {store, api, set, response} = setup(false);
        store.form.patchValue({remember: true, password: ' old password '});
        expect(await store.submit()).toBe(true);
        expect(api.login).toHaveBeenCalledExactlyOnceWith({username: 'an_nguyen', password: ' old password '});
        expect(set).toHaveBeenCalledExactlyOnceWith(response, true);
    });

    it('blocks duplicate submissions while the request is pending', async () => {
        const {store, api} = setup(false);
        let resolve!: (value: unknown) => void;
        api.login.mockReturnValueOnce(new Promise((done) => {
            resolve = done;
        }));
        const pending = store.submit();
        expect(store.busy()).toBe(true);
        expect(await store.submit()).toBe(false);
        expect(api.login).toHaveBeenCalledTimes(1);
        resolve({});
        await pending;
        expect(store.busy()).toBe(false);
    });

    it.each([
        [401, 'Tên đăng nhập hoặc mật khẩu chưa đúng'],
        [0, 'Không thể kết nối máy chủ'],
    ])('shows a recoverable login error for HTTP %s', async (status, message) => {
        const {store, api, set} = setup(false);
        api.login.mockRejectedValueOnce(new HttpErrorResponse({status}));
        expect(await store.submit()).toBe(false);
        expect(store.error()).toContain(message);
        expect(set).not.toHaveBeenCalled();
    });
});

describe('authReturnUrl', () => {
    it.each(['https://example.com', '//example.com', '/\\example.com', '/login', '/register', '/quiz/id?redirect=https://example.com', null])('rejects unsafe or recursive destination %s', (value) => {
        expect(authReturnUrl(value)).toBe('/');
    });

    it.each(['/quiz/abc-123', '/quiz/abc-123/attempt'])('keeps a quiz destination %s', (value) => {
        expect(authReturnUrl(value)).toBe(value);
    });
});
