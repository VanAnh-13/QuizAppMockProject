import {inject} from '@angular/core';
import {CanActivateFn, Router} from '@angular/router';
import {authReturnUrl} from '../../features/auth/domain/auth-contracts';
import {AuthSession} from './auth-session';

export const requireAdminGuard: CanActivateFn = (_route, state) => {
    const session = inject(AuthSession);
    if (session.isAdmin()) return true;

    if (!session.token()) {
        return inject(Router).createUrlTree(['/login'], {
            queryParams: {returnUrl: authReturnUrl(state.url)},
        });
    }

    return inject(Router).createUrlTree(['/']);
};
