import {TestBed} from '@angular/core/testing';
import {ActivatedRouteSnapshot, provideRouter, Router, RouterStateSnapshot, UrlTree} from '@angular/router';
import {AuthSession} from './auth-session';
import {requireAuthGuard} from './require-auth.guard';

describe('requireAuthGuard', () => {
    const returnUrl = '/quiz/quiz-123/attempt?attemptId=attempt-456';

    beforeEach(() => {
        sessionStorage.clear();
        localStorage.clear();
        TestBed.configureTestingModule({providers: [provideRouter([])]});
    });

    afterEach(() => {
        sessionStorage.clear();
        localStorage.clear();
    });

    function activate() {
        return TestBed.runInInjectionContext(() =>
            requireAuthGuard(new ActivatedRouteSnapshot(), {url: returnUrl} as RouterStateSnapshot));
    }

    function setSession(expiresAt: Date) {
        TestBed.inject(AuthSession).set({
            token: 'test-token',
            expiresAt: expiresAt.toISOString(),
            userDto: {id: 'learner-id', username: 'learner', fullName: null},
        });
    }

    it('redirects anonymous visitors and preserves their attempt', () => {
        const result = activate();

        expect(result).toBeInstanceOf(UrlTree);
        expect(TestBed.inject(Router).serializeUrl(result as UrlTree)).toBe(
            `/login?returnUrl=${encodeURIComponent(returnUrl)}`,
        );
    });

    it('allows a valid signed-in session to open the attempt', () => {
        setSession(new Date(Date.now() + 60_000));

        expect(activate()).toBe(true);
    });

    it('redirects an expired session even when its profile is still present', () => {
        setSession(new Date(Date.now() - 1_000));
        expect(TestBed.inject(AuthSession).user()).not.toBeNull();

        expect(activate()).toBeInstanceOf(UrlTree);
        expect(TestBed.inject(AuthSession).user()).toBeNull();
    });
});
