import {inject} from '@angular/core';
import {CanActivateFn, Router} from '@angular/router';
import {authReturnUrl} from '../../features/auth/domain/auth-contracts';
import {AuthSession} from './auth-session';

function tokenRoles(token: string): readonly string[] {
    try {
        const payload = token.split('.')[1];
        if (!payload) return [];
        const json = JSON.parse(atob(payload.replace(/-/g, '+').replace(/_/g, '/'))) as Record<string, unknown>;
        // noinspection HttpUrlsUsage
        const roleClaimType = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';
        const value = json['role'] ?? json[roleClaimType];
        return Array.isArray(value)
            ? value.filter((role): role is string => typeof role === 'string')
            : typeof value === 'string'
              ? [value]
              : [];
    } catch {
        return [];
    }
}

export const requireAdminGuard: CanActivateFn = (_route, state) => {
    const session = inject(AuthSession);
    const token = session.token();
    if (token && tokenRoles(token).includes('Admin')) return true;

    if (!token) {
        return inject(Router).createUrlTree(['/login'], {
            queryParams: {returnUrl: authReturnUrl(state.url)},
        });
    }

    return inject(Router).createUrlTree(['/']);
};
