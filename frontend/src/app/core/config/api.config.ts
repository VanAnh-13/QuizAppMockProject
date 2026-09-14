import {InjectionToken} from '@angular/core';

export interface ApiConfig {
    readonly baseUrl: string;
    readonly pageSize: number;
}

export const API_CONFIG = new InjectionToken<ApiConfig>('API_CONFIG', {
    providedIn: 'root',
    factory: () => ({baseUrl: '/api', pageSize: 100}),
});
