import {InjectionToken} from '@angular/core';

export interface AccountConfig {
    readonly pageSize: number;
}

export const ACCOUNT_CONFIG = new InjectionToken<AccountConfig>('ACCOUNT_CONFIG', {
    providedIn: 'root',
    factory: () => ({pageSize: 8}),
});
