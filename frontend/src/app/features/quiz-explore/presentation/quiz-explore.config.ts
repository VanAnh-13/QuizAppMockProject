import {InjectionToken} from '@angular/core';

export interface QuizExploreConfig {
    readonly pageSize: number;
}

export const QUIZ_EXPLORE_CONFIG = new InjectionToken<QuizExploreConfig>('QUIZ_EXPLORE_CONFIG', {
    providedIn: 'root',
    factory: () => ({pageSize: 6}),
});
