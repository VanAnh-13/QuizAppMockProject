import {InjectionToken} from '@angular/core';
import {
    AccountProfile,
    AttemptHistoryPage,
    ChangePasswordRequest,
} from '../domain/account-contracts';

export interface AccountApi {
    profile(): Promise<AccountProfile>;

    history(pageNumber: number, pageSize: number): Promise<AttemptHistoryPage>;

    changePassword(request: ChangePasswordRequest): Promise<void>;
}

export const ACCOUNT_API = new InjectionToken<AccountApi>('ACCOUNT_API');
