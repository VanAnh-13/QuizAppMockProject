import {InjectionToken} from '@angular/core';
import {AdminRole, AdminRolePage, AdminUser, AdminUserPage} from '../domain/admin-user';

export interface AdminUserApi {
    listUsers(pageNumber: number, pageSize: number, search: string): Promise<AdminUserPage>;

    setUserActive(id: string, isActive: boolean): Promise<void>;

    updateUserRoles(id: string, roleIds: readonly string[]): Promise<void>;

    listRoles(pageNumber: number, pageSize: number, search: string): Promise<AdminRolePage>;

    updateRole(
        id: string,
        draft: { roleName: string; description: string | null; isActive: boolean },
    ): Promise<void>;

    deleteRole(id: string): Promise<void>;
}

export const ADMIN_USER_API = new InjectionToken<AdminUserApi>('ADMIN_USER_API');
export const ADMIN_USER_CONFIG = new InjectionToken<{ pageSize: number }>('ADMIN_USER_CONFIG', {
    providedIn: 'root',
    factory: () => ({pageSize: 5}),
});
