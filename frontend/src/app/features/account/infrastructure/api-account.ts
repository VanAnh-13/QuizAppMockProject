import {inject, Injectable} from '@angular/core';
import {ApiClient, PagedResult} from '../../../core/api/api-client';
import {AccountApi} from '../application/account-api';
import {
    AccountProfile,
    AttemptHistoryEntry,
    AttemptHistoryPage,
    ChangePasswordRequest,
} from '../domain/account-contracts';

interface ProfilePayload {
    readonly id: string;
    readonly username: string;
    readonly email: string;
    readonly fullName: string | null;
    readonly phoneNumber: string | null;
    readonly dateOfBirth: string | null;
    readonly roles: readonly { readonly id: string; readonly roleName: string }[];
}

@Injectable()
export class ApiAccount implements AccountApi {
    private readonly api = inject(ApiClient);

    async profile(): Promise<AccountProfile> {
        return validateProfile(await this.api.get<ProfilePayload>('auth/me'));
    }

    async history(pageNumber: number, pageSize: number): Promise<AttemptHistoryPage> {
        const page = await this.api.get<PagedResult<AttemptHistoryEntry>>('quiz-history', {
            pageNumber,
            pageSize,
        });

        if (
            !Array.isArray(page?.items) ||
            !Number.isInteger(page.totalCount) ||
            page.totalCount < 0
        ) {
            throw new Error('Dữ liệu lịch sử làm bài không hợp lệ.');
        }

        return {
            items: page.items.map(validateHistoryEntry),
            totalCount: page.totalCount,
            pageNumber: Number.isInteger(page.pageNumber) ? page.pageNumber : pageNumber,
            pageSize: Number.isInteger(page.pageSize) ? page.pageSize : pageSize,
        };
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
                .map((role) => ({id: role.id, roleName: role.roleName}))
            : [],
    };
}

function validateHistoryEntry(entry: AttemptHistoryEntry): AttemptHistoryEntry {
    if (
        !entry ||
        typeof entry.id !== 'string' ||
        typeof entry.quizId !== 'string' ||
        typeof entry.quizTitle !== 'string' ||
        !Number.isFinite(Date.parse(entry.submittedAt)) ||
        !Number.isFinite(entry.score)
    ) {
        throw new Error('Lượt làm bài không hợp lệ.');
    }

    return {...entry, passedScore: Number.isFinite(entry.passedScore) ? entry.passedScore : null};
}
