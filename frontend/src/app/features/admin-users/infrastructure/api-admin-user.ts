import {inject, Injectable} from '@angular/core';
import {ApiClient, PagedResult} from '../../../core/api/api-client';
import {listParams, validatePagedResult} from '../../../core/api/paged-result';
import {AdminUserApi} from '../application/admin-user-api';
import {AdminRole, AdminRolePage, AdminUser, AdminUserPage} from '../domain/admin-user';

interface RolePayload {
    readonly id: string;
    readonly roleName: string;
    readonly description?: string | null;
    readonly isActive: boolean;
}

interface UserPayload {
    readonly id: string;
    readonly username: string;
    readonly email: string;
    readonly fullName?: string | null;
    readonly phoneNumber?: string | null;
    readonly dateOfBirth?: string | null;
    readonly avatar?: string | null;
    readonly isActive: boolean;
    readonly roles?: readonly RolePayload[];
}

@Injectable()
export class ApiAdminUser implements AdminUserApi {
    private readonly api = inject(ApiClient);

    async listUsers(pageNumber: number, pageSize: number, search: string): Promise<AdminUserPage> {
        const page = validatePagedResult(
            await this.api.get<PagedResult<UserPayload>>('users', listParams(pageNumber, pageSize, search)),
            pageNumber,
            pageSize,
            'Invalid user data.',
        );
        return {...page, items: page.items.map(validateUser)};
    }

    async setUserActive(id: string, isActive: boolean): Promise<void> {
        await this.api.patch<void>(`users/${id}/activate`, {isActive});
    }

    async updateUserRoles(id: string, roleIds: readonly string[]): Promise<void> {
        await this.api.put<void>(`users/${id}/roles`, {roleIds});
    }

    async listRoles(pageNumber: number, pageSize: number, search: string): Promise<AdminRolePage> {
        const page = validatePagedResult(
            await this.api.get<PagedResult<RolePayload>>('roles', listParams(pageNumber, pageSize, search)),
            pageNumber,
            pageSize,
            'Invalid role data.',
        );
        return {...page, items: page.items.map(validateRole)};
    }

    async updateRole(
        id: string,
        draft: { roleName: string; description: string | null; isActive: boolean },
    ): Promise<void> {
        await this.api.put<void>(`roles/${id}`, draft);
    }

    async deleteRole(id: string): Promise<void> {
        await this.api.delete<void>(`roles/${id}`);
    }
}

function validateRole(role: RolePayload): AdminRole {
    if (!role || typeof role.id !== 'string' || typeof role.roleName !== 'string') {
        throw new Error('Invalid role.');
    }
    return {
        id: role.id,
        roleName: role.roleName,
        description: englishRoleDescription(role),
        isActive: Boolean(role.isActive),
    };
}

function englishRoleDescription(role: RolePayload): string | null {
    // Translate only the original seeded descriptions; preserve administrator-authored content.
    if (role.roleName === 'Admin' && role.description === 'Quản trị viên hệ thống') {
        return 'System administrator';
    }
    if (role.roleName === 'User' && role.description === 'Người dùng tham gia bài thi') {
        return 'Quiz participant';
    }
    return role.description ?? null;
}

function validateUser(user: UserPayload): AdminUser {
    if (!user || typeof user.id !== 'string' || typeof user.username !== 'string' || typeof user.email !== 'string') {
        throw new Error('Invalid user.');
    }
    return {
        id: user.id,
        username: user.username,
        email: user.email,
        fullName: user.fullName ?? null,
        phoneNumber: user.phoneNumber ?? null,
        dateOfBirth: user.dateOfBirth ?? null,
        avatar: user.avatar ?? null,
        isActive: Boolean(user.isActive),
        roles: Array.isArray(user.roles) ? user.roles.map(validateRole) : [],
    };
}
