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

    it('requests a page of submitted attempts', async () => {
        const entry = {
            id: 'attempt-id',
            quizId: 'quiz-id',
            quizTitle: 'C# Căn bản',
            submittedAt: '2024-05-24T14:35:00Z',
            score: 82.5,
            passedScore: 70,
        };
        const {account, api} = setup({items: [entry], totalCount: 12, pageNumber: 2, pageSize: 8});

        const page = await account.history(2, 8);

        expect(api.get).toHaveBeenCalledWith('quiz-history', {pageNumber: 2, pageSize: 8});
        expect(page).toEqual({items: [entry], totalCount: 12, pageNumber: 2, pageSize: 8});
    });

    it('treats a missing pass mark as not configured', async () => {
        const {account} = setup({
            items: [
                {
                    id: 'attempt-id',
                    quizId: 'quiz-id',
                    quizTitle: 'C# Căn bản',
                    submittedAt: '2024-05-24T14:35:00Z',
                    score: 65,
                    passedScore: null,
                },
            ],
            totalCount: 1,
            pageNumber: 1,
            pageSize: 8,
        });

        const page = await account.history(1, 8);

        expect(page.items[0]?.passedScore).toBeNull();
    });

    it.each([
        {items: 'not-an-array', totalCount: 1, pageNumber: 1, pageSize: 8},
        {items: [], totalCount: -1, pageNumber: 1, pageSize: 8},
    ])('rejects an invalid history page %s', async (payload) => {
        const {account} = setup(payload);

        await expect(account.history(1, 8)).rejects.toThrow(
            'Dữ liệu lịch sử làm bài không hợp lệ.',
        );
    });

    it('rejects an attempt row without a submission timestamp', async () => {
        const {account} = setup({
            items: [{id: 'a', quizId: 'q', quizTitle: 'T', submittedAt: 'not-a-date', score: 1}],
            totalCount: 1,
            pageNumber: 1,
            pageSize: 8,
        });

        await expect(account.history(1, 8)).rejects.toThrow('Lượt làm bài không hợp lệ.');
    });

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
