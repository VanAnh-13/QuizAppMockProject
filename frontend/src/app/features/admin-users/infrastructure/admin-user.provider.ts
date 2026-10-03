import {Provider} from '@angular/core';
import {ADMIN_USER_API} from '../application/admin-user-api';
import {ApiAdminUser} from './api-admin-user';

export const adminUserProvider: Provider = {provide: ADMIN_USER_API, useClass: ApiAdminUser};
