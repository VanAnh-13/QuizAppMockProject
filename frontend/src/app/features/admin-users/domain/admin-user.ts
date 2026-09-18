export interface AdminRole {
    readonly id: string;
    readonly roleName: string;
    readonly description: string | null;
    readonly isActive: boolean;
}

export interface AdminRolePage {
    readonly items: readonly AdminRole[];
    readonly totalCount: number;
    readonly pageNumber: number;
    readonly pageSize: number;
}

export interface AdminUser {
    readonly id: string;
    readonly username: string;
    readonly email: string;
    readonly fullName: string | null;
    readonly phoneNumber: string | null;
    readonly dateOfBirth: string | null;
    readonly avatar: string | null;
    readonly isActive: boolean;
    readonly roles: readonly AdminRole[];
}

export interface AdminUserPage {
    readonly items: readonly AdminUser[];
    readonly totalCount: number;
    readonly pageNumber: number;
    readonly pageSize: number;
}
