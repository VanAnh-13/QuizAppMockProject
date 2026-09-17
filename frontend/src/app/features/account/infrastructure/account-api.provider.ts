import {Provider} from '@angular/core';
import {ACCOUNT_API} from '../application/account-api';
import {ApiAccount} from './api-account';

export const accountApiProvider: Provider = {
    provide: ACCOUNT_API,
    useClass: ApiAccount,
};
