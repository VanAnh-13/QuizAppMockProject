export interface AccountRole {
    readonly id: string;
    readonly roleName: string;
}

export interface AccountProfile {
    readonly id: string;
    readonly username: string;
    readonly email: string;
    readonly fullName: string | null;
    readonly phoneNumber: string | null;
    readonly dateOfBirth: string | null;
    readonly roles: readonly AccountRole[];
}

export interface AttemptHistoryEntry {
    readonly id: string;
    readonly quizId: string;
    readonly quizTitle: string;
    readonly submittedAt: string;
    readonly score: number;
    readonly passedScore: number | null;
}

export interface AttemptHistoryPage {
    readonly items: readonly AttemptHistoryEntry[];
    readonly totalCount: number;
    readonly pageNumber: number;
    readonly pageSize: number;
}

export interface ChangePasswordRequest {
    readonly currentPassword: string;
    readonly newPassword: string;
    readonly confirmNewPassword: string;
}

export function accountInitials(profile: AccountProfile): string {
    const source = profile.fullName?.trim() || profile.username;

    return source
        .split(/\s+/)
        .filter((part) => part.length > 0)
        .slice(-2)
        .map((part) => part[0]!.toLocaleUpperCase('vi-VN'))
        .join('');
}

export function formatScore(score: number): string {
    return score.toLocaleString('vi-VN', {maximumFractionDigits: 1});
}
