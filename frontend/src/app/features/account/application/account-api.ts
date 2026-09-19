import {InjectionToken} from '@angular/core';
import {
    AccountProfile,
    ChangePasswordRequest,
} from '../domain/account-contracts';

export interface AccountApi {
    profile(): Promise<AccountProfile>;

    changePassword(request: ChangePasswordRequest): Promise<void>;
}

export const ACCOUNT_API = new InjectionToken<AccountApi>('ACCOUNT_API');
