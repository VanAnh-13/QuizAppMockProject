import {TestBed} from '@angular/core/testing';
import {ActivatedRouteSnapshot, provideRouter, Router, RouterStateSnapshot, UrlTree} from '@angular/router';
import {AuthSession} from './auth-session';
import {requireAdminGuard} from './require-admin.guard';

function jwtWithRoles(roles: readonly string[]): string {
    const payload = btoa(JSON.stringify({role: roles.length === 1 ? roles[0] : roles}))
        .replace(/\+/g, '-')
        .replace(/\//g, '_')
        .replace(/=+$/, '');
    return `aaa.${payload}.bbb`;
}

describe('requireAdminGuard', () => {
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
            requireAdminGuard(new ActivatedRouteSnapshot(), {
                url: '/admin/questions',
            } as RouterStateSnapshot),
        );
    }

    function setSession(token: string) {
        TestBed.inject(AuthSession).set({
            token,
            expiresAt: new Date(Date.now() + 60_000).toISOString(),
            userDto: {id: 'admin-id', username: 'admin', fullName: 'Admin'},
        });
    }

    it('sends anonymous visitors to login', () => {
        const result = activate();
        expect(result).toBeInstanceOf(UrlTree);
        expect(TestBed.inject(Router).serializeUrl(result as UrlTree)).toBe(
            `/login?returnUrl=${encodeURIComponent('/admin/questions')}`,
        );
    });

    it('allows an administrator token through', () => {
        setSession(jwtWithRoles(['Admin']));
        expect(activate()).toBe(true);
    });

    it('allows a token that uses the Microsoft role claim type', () => {
        const payload = btoa(
            JSON.stringify({
                'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': 'Admin',
            }),
        )
            .replace(/\+/g, '-')
            .replace(/\//g, '_')
            .replace(/=+$/, '');
        setSession(`aaa.${payload}.bbb`);
        expect(activate()).toBe(true);
    });

    it('sends a signed-in learner home', () => {
        setSession(jwtWithRoles(['Student']));
        const result = activate();
        expect(result).toBeInstanceOf(UrlTree);
        expect(TestBed.inject(Router).serializeUrl(result as UrlTree)).toBe('/');
    });
});
