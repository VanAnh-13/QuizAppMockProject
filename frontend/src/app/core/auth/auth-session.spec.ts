import {HttpClient, provideHttpClient, withInterceptors} from '@angular/common/http';
import {HttpTestingController, provideHttpClientTesting} from '@angular/common/http/testing';
import {TestBed} from '@angular/core/testing';
import {API_CONFIG} from '../config/api.config';
import {AuthResponse, AuthSession} from './auth-session';
import {authInterceptor} from './auth.interceptor';

const signedInAt = new Date('2026-09-13T08:00:00Z');
const response: AuthResponse = {
    token: 'test-token',
    expiresAt: '2026-09-13T09:00:00Z',
    userDto: {
        id: 'test-user',
        username: 'learner',
        fullName: 'Quiz Learner',
    },
};

describe('AuthSession expiration', () => {
    beforeEach(() => {
        sessionStorage.clear();
        localStorage.clear();
        vi.useFakeTimers();
        vi.setSystemTime(signedInAt);
    });

    afterEach(() => {
        vi.restoreAllMocks();
        vi.useRealTimers();
        sessionStorage.clear();
        localStorage.clear();
    });

    it('restores a remembered session after a new service instance is created', () => {
        const session = TestBed.inject(AuthSession);
        session.set(response, true);
        expect(sessionStorage.getItem('quizapp.session')).toBeNull();
        expect(TestBed.runInInjectionContext(() => new AuthSession()).token()).toBe(response.token);
    });

    it('removes the remembered session when a later login is not remembered', () => {
        const session = TestBed.inject(AuthSession);
        session.set(response, true);
        session.set({...response, token: 'temporary-token'});
        expect(localStorage.getItem('quizapp.session')).toBeNull();
        expect(TestBed.runInInjectionContext(() => new AuthSession()).token()).toBe('temporary-token');
    });

    it('removes both storage entries on logout', () => {
        const session = TestBed.inject(AuthSession);
        session.set(response, true);
        session.clear();
        expect(localStorage.getItem('quizapp.session')).toBeNull();
        expect(sessionStorage.getItem('quizapp.session')).toBeNull();
        expect(TestBed.runInInjectionContext(() => new AuthSession()).user()).toBeNull();
    });

    it('does not restore expired or malformed remembered sessions', () => {
        localStorage.setItem('quizapp.session', 'invalid-json');
        expect(TestBed.runInInjectionContext(() => new AuthSession()).token()).toBeNull();
        localStorage.setItem('quizapp.session', JSON.stringify(response));
        vi.setSystemTime(new Date(response.expiresAt));
        expect(TestBed.runInInjectionContext(() => new AuthSession()).token()).toBeNull();
    });

    it('removes stale localStorage entry when restoring an expired remembered session', () => {
        localStorage.setItem('quizapp.session', JSON.stringify(response));
        vi.setSystemTime(new Date(response.expiresAt));

        TestBed.runInInjectionContext(() => new AuthSession());

        expect(localStorage.getItem('quizapp.session')).toBeNull();
    });

    it('keeps memory login working when browser storage is blocked', () => {
        vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
            throw new Error('Storage unavailable');
        });
        const session = TestBed.inject(AuthSession);
        session.set(response, true);
        expect(session.token()).toBe(response.token);
    });

    it('keeps the user and token while the session is valid', () => {
        const session = TestBed.inject(AuthSession);
        session.set(response);

        expect(session.token()).toBe(response.token);
        expect(session.user()).toEqual(response.userDto);
    });

    it('clears the cached user and stored session at the expiration deadline', () => {
        const session = TestBed.inject(AuthSession);
        session.set(response);
        expect(session.user()).toEqual(response.userDto);

        vi.setSystemTime(new Date(response.expiresAt));

        expect(session.token()).toBeNull();
        expect(session.user()).toBeNull();
        expect(sessionStorage.getItem('quizapp.session')).toBeNull();
    });

    it('clears the expired user even when storage removal fails', () => {
        const session = TestBed.inject(AuthSession);
        session.set(response);
        expect(session.user()).toEqual(response.userDto);
        vi.spyOn(Storage.prototype, 'removeItem').mockImplementation(() => {
            throw new Error('Storage unavailable');
        });

        vi.setSystemTime(new Date(response.expiresAt));

        expect(session.token()).toBeNull();
        expect(session.user()).toBeNull();
    });

    it('clears expired auth before an unauthenticated request and preserves a later login on 401', () => {
        TestBed.configureTestingModule({
            providers: [
                provideHttpClient(withInterceptors([authInterceptor])),
                provideHttpClientTesting(),
            ],
        });

        const session = TestBed.inject(AuthSession);
        const http = TestBed.inject(HttpClient);
        const controller = TestBed.inject(HttpTestingController);
        const baseUrl = TestBed.inject(API_CONFIG).baseUrl.replace(/\/$/, '');
        const url = `${baseUrl}/quiz-history`;
        const onError = vi.fn();

        session.set(response);
        expect(session.user()).toEqual(response.userDto);
        vi.setSystemTime(new Date(response.expiresAt));

        http.get(url).subscribe({error: onError});

        const request = controller.expectOne(url);
        expect(request.request.headers.has('Authorization')).toBe(false);
        expect(session.user()).toBeNull();
        expect(sessionStorage.getItem('quizapp.session')).toBeNull();

        const renewedSession = {
            ...response,
            token: 'renewed-token',
            expiresAt: '2026-09-13T10:00:00Z',
        };
        session.set(renewedSession);
        request.flush({}, {status: 401, statusText: 'Unauthorized'});

        expect(onError).toHaveBeenCalledOnce();
        expect(session.token()).toBe(renewedSession.token);
        expect(session.user()).toEqual(renewedSession.userDto);
        controller.verify();
    });

    it.each(['login', 'register', 'login?source=quiz', 'register?source=quiz'])(
        'omits the bearer token for public auth route %s and preserves the session on 401', (route) => {
            TestBed.configureTestingModule({
                providers: [
                    provideHttpClient(withInterceptors([authInterceptor])),
                    provideHttpClientTesting(),
                ],
            });

            const session = TestBed.inject(AuthSession);
            const http = TestBed.inject(HttpClient);
            const controller = TestBed.inject(HttpTestingController);
            const baseUrl = TestBed.inject(API_CONFIG).baseUrl.replace(/\/$/, '');
            const onError = vi.fn();

            session.set(response);
            expect(session.token()).toBe(response.token);

            http.post(`${baseUrl}/auth/${route}`, {username: 'other', password: 'wrong'}).subscribe({error: onError});

            const request = controller.expectOne(`${baseUrl}/auth/${route}`);
            expect(request.request.headers.has('Authorization')).toBe(false);

            request.flush({}, {status: 401, statusText: 'Unauthorized'});

            expect(onError).toHaveBeenCalledOnce();
            expect(session.token()).toBe(response.token);
            expect(session.user()).toEqual(response.userDto);
            controller.verify();
        });

    it.each(['change-password', 'change-password?source=profile', 'login/audit'])(
        'attaches the bearer token to protected auth route %s', (route) => {
            TestBed.configureTestingModule({
                providers: [
                    provideHttpClient(withInterceptors([authInterceptor])),
                    provideHttpClientTesting(),
                ],
            });

            const session = TestBed.inject(AuthSession);
            const http = TestBed.inject(HttpClient);
            const controller = TestBed.inject(HttpTestingController);
            const baseUrl = TestBed.inject(API_CONFIG).baseUrl.replace(/\/$/, '');
            const url = `${baseUrl}/auth/${route}`;

            session.set(response);
            http.post(url, {}).subscribe();

            const request = controller.expectOne(url);
            request.flush(null, {status: 204, statusText: 'No Content'});

            expect(request.request.method).toBe('POST');
            expect(request.request.headers.get('Authorization')).toBe(`Bearer ${response.token}`);
            controller.verify();
        });
});
