export interface LoginRequest {
    readonly username: string;
    readonly password: string;
}

export interface RegisterRequest extends LoginRequest {
    readonly email: string;
    readonly confirmPassword: string;
    readonly profile: {
        readonly fullName: string;
        readonly phoneNumber?: string | null;
        readonly dateOfBirth?: string | null;
    };
}

export interface RoleDto {
    readonly id: string;
    readonly roleName: string;
    readonly description: string | null;
    readonly isActive: boolean;
}

export interface UserDto {
    readonly id: string;
    readonly username: string;
    readonly email: string;
    readonly fullName: string | null;
    readonly phoneNumber: string | null;
    readonly dateOfBirth: string | null;
    readonly avatar: string | null;
    readonly isActive: boolean;
    readonly createdAt: string;
    readonly updatedAt: string;
    readonly roles: readonly RoleDto[];
}

export const AUTH_LIMITS = {
    username: 100,
    fullName: 200,
    email: 256,
    phoneNumber: 32,
    passwordMin: 8,
    passwordMax: 128,
} as const;

export function authReturnUrl(value: string | null): string {
    return value && /^\/quiz\/[a-zA-Z0-9-]+(?:\/attempt(?:\?attemptId=[a-zA-Z0-9-]+)?)?$/.test(value) ? value : '/';
}
