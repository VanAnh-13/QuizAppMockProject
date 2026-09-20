import {InjectionToken} from '@angular/core';

export interface ContactConfig {
    readonly email?: string;
}

export const CONTACT_CONFIG = new InjectionToken<ContactConfig>('CONTACT_CONFIG', {
    providedIn: 'root',
    factory: () => ({email: undefined}),
});
