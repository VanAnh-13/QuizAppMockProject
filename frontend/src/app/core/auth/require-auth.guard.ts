import {inject} from '@angular/core';
import {CanActivateFn, Router} from '@angular/router';
import {authReturnUrl} from '../../features/auth/domain/auth-contracts';
import {AuthSession} from './auth-session';

export const requireAuthGuard: CanActivateFn = (_route, state) => {
    if (inject(AuthSession).token()) return true;

    return inject(Router).createUrlTree(['/login'], {
        queryParams: {returnUrl: authReturnUrl(state.url)},
    });
};
