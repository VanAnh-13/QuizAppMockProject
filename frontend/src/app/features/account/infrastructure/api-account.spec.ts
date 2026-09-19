import {TestBed} from '@angular/core/testing';
import {ApiClient} from '../../../core/api/api-client';
import {ApiAccount} from './api-account';

describe('ApiAccount', () => {
    function setup(get: unknown, post: unknown = undefined) {
        const api = {
            get: vi.fn().mockResolvedValue(get),
            post: vi.fn().mockResolvedValue(post),
        };
        TestBed.configureTestingModule({
            providers: [ApiAccount, {provide: ApiClient, useValue: api}],
        });

        return {account: TestBed.inject(ApiAccount), api};
    }

    const profilePayload = {
        id: 'user-id',
        username: 'tuan.nguyen',
        email: 'tuan.nguyen@example.com',
        fullName: 'Nguyễn Minh Tuấn',
        phoneNumber: '0912345678',
        dateOfBirth: '1996-08-15',
        roles: [{id: 'role-id', roleName: 'Admin'}],
    };

    it('reads the signed-in profile from the authenticated endpoint', async () => {
        const {account, api} = setup(profilePayload);

        const profile = await account.profile();

        expect(api.get).toHaveBeenCalledWith('auth/me');
        expect(profile).toEqual({
            id: 'user-id',
            username: 'tuan.nguyen',
            email: 'tuan.nguyen@example.com',
            fullName: 'Nguyễn Minh Tuấn',
            phoneNumber: '0912345678',
            dateOfBirth: '1996-08-15',
            roles: [{id: 'role-id', roleName: 'Admin'}],
        });
    });

    it('normalizes optional profile fields and missing roles', async () => {
        const {account} = setup({id: 'user-id', username: 'student', email: 'student@example.com'});

        const profile = await account.profile();

        expect(profile.fullName).toBeNull();
        expect(profile.phoneNumber).toBeNull();
        expect(profile.dateOfBirth).toBeNull();
        expect(profile.roles).toEqual([]);
    });

    it.each([{}, {id: 'user-id'}, {id: 1, username: 'a', email: 'b'}])(
        'rejects an invalid profile payload %s',
        async (payload) => {
            const {account} = setup(payload);

            await expect(account.profile()).rejects.toThrow('Dữ liệu hồ sơ không hợp lệ.');
        },
    );

    it('posts the password change to the authenticated endpoint', async () => {
        const {account, api} = setup(undefined);
        const request = {
            currentPassword: 'Original-password-123!',
            newPassword: 'Changed-password-456!',
            confirmNewPassword: 'Changed-password-456!',
        };

        await account.changePassword(request);

        expect(api.post).toHaveBeenCalledWith('auth/change-password', request);
    });
});
