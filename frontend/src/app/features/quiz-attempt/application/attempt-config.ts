import { InjectionToken } from '@angular/core';

export const ATTEMPT_CONFIG = new InjectionToken<{
  readonly saveDelayMs: number;
  readonly tickMs: number;
}>('ATTEMPT_CONFIG', {
  providedIn: 'root',
  factory: () => ({ saveDelayMs: 400, tickMs: 250 }),
});
