import {TestBed} from '@angular/core/testing';
import {ApiClient} from '../../../core/api/api-client';
import {ApiAdminUser} from './api-admin-user';

describe('ApiAdminUser', () => {
    it('translates original seeded role descriptions and preserves custom descriptions', async () => {
        const items = [
            {id: 'admin', roleName: 'Admin', description: 'Quản trị viên hệ thống', isActive: true},
            {id: 'user', roleName: 'User', description: 'Người dùng tham gia bài thi', isActive: true},
            {id: 'custom', roleName: 'Editor', description: 'Nội dung do quản trị viên nhập', isActive: true},
        ];
        TestBed.configureTestingModule({
            providers: [ApiAdminUser, {
                provide: ApiClient,
                useValue: {get: vi.fn().mockResolvedValue({items, pageNumber: 1, pageSize: 5, totalCount: 3})},
            }]
        });

        const page = await TestBed.inject(ApiAdminUser).listRoles(1, 5, '');

        expect(page.items.map((role) => role.description)).toEqual([
            'System administrator', 'Quiz participant', 'Nội dung do quản trị viên nhập',
        ]);
    });

    it('updates user roles and deletes a role', async () => {
        const put = vi.fn().mockResolvedValue(undefined);
        const del = vi.fn().mockResolvedValue(undefined);
        TestBed.configureTestingModule({
            providers: [ApiAdminUser, {provide: ApiClient, useValue: {put, delete: del}}],
        });
        const api = TestBed.inject(ApiAdminUser);

        await api.updateUserRoles('user-1', ['role-a', 'role-b']);
        await api.deleteRole('role-a');

        expect(put).toHaveBeenCalledWith('users/user-1/roles', {roleIds: ['role-a', 'role-b']});
        expect(del).toHaveBeenCalledWith('roles/role-a');
    });
});
