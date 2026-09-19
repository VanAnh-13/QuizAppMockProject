import {inject, Injectable} from '@angular/core';
import {ApiClient} from '../../../core/api/api-client';
import {RoleDto} from '../../auth/domain/auth-contracts';
import {AccountApi} from '../application/account-api';
import {
    AccountProfile,
    ChangePasswordRequest,
} from '../domain/account-contracts';

interface ProfilePayload {
    readonly id: string;
    readonly username: string;
    readonly email: string;
    readonly fullName: string | null;
    readonly phoneNumber: string | null;
    readonly dateOfBirth: string | null;
    readonly roles: readonly RoleDto[];
}

@Injectable()
export class ApiAccount implements AccountApi {
    private readonly api = inject(ApiClient);

    async profile(): Promise<AccountProfile> {
        return validateProfile(await this.api.get<ProfilePayload>('auth/me'));
    }

    async changePassword(request: ChangePasswordRequest): Promise<void> {
        await this.api.post<void>('auth/change-password', request);
    }
}

function validateProfile(profile: ProfilePayload): AccountProfile {
    if (
        !profile ||
        typeof profile.id !== 'string' ||
        typeof profile.username !== 'string' ||
        typeof profile.email !== 'string'
    ) {
        throw new Error('Dữ liệu hồ sơ không hợp lệ.');
    }

    return {
        id: profile.id,
        username: profile.username,
        email: profile.email,
        fullName: profile.fullName ?? null,
        phoneNumber: profile.phoneNumber ?? null,
        dateOfBirth: profile.dateOfBirth ?? null,
        roles: Array.isArray(profile.roles)
            ? profile.roles
                .filter((role) => typeof role?.roleName === 'string')
                .map((role) => ({id: role.id, roleName: role.roleName, isActive: role.isActive === true}))
            : [],
    };
}
