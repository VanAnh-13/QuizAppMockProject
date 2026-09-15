import {HttpClient, provideHttpClient, withInterceptors} from '@angular/common/http';
import {HttpTestingController, provideHttpClientTesting} from '@angular/common/http/testing';
import {TestBed} from '@angular/core/testing';
import {API_CONFIG} from '../config/api.config';
import {AuthResponse, AuthSession} from './auth-session';
import {authInterceptor} from './auth.interceptor';

const sessionKey = 'quizapp.session';
const response: AuthResponse = {
    token: 'remembered-test-token',
    expiresAt: '2030-01-01T01:00:00Z',
    userDto: {id: 'learner', username: 'learner', fullName: 'Quiz Learner'},
};

describe('AuthSession across tabs', () => {
    beforeEach(() => {
        sessionStorage.clear();
        localStorage.clear();
        vi.useFakeTimers();
        vi.setSystemTime(new Date('2030-01-01T00:00:00Z'));
    });

    afterEach(() => {
        TestBed.resetTestingModule();
        vi.restoreAllMocks();
        vi.useRealTimers();
        sessionStorage.clear();
        localStorage.clear();
    });

    it.each([false, true])('clears the user immediately after another tab logs out (restored: %s)', (restored) => {
        if (restored) {
            localStorage.setItem(sessionKey, JSON.stringify(response));
        }
        const session = TestBed.inject(AuthSession);
        if (!restored) {
            session.set(response, true);
        }
        expect(session.user()).toEqual(response.userDto);

        localStorage.removeItem(sessionKey);
        notifyStorage(sessionKey);

        expect(session.user()).toBeNull();
        expect(session.token()).toBeNull();
    });

    it('clears a remembered session when another tab clears local storage', () => {
        const session = TestBed.inject(AuthSession);
        session.set(response, true);

        localStorage.clear();
        notifyStorage(null);

        expect(session.user()).toBeNull();
        expect(session.token()).toBeNull();
    });

    it('invalidates a replaced session without deleting the other tab account', () => {
        const session = TestBed.inject(AuthSession);
        session.set(response, true);
        const replacement = JSON.stringify({...response, token: 'other-account-token'});
        localStorage.setItem(sessionKey, replacement);
        notifyStorage(sessionKey, replacement);

        expect(session.user()).toBeNull();
        expect(session.token()).toBeNull();
        expect(localStorage.getItem(sessionKey)).toBe(replacement);
    });

    it('keeps a newer local login when an older storage event is delivered late', () => {
        const session = TestBed.inject(AuthSession);
        session.set(response, true);
        localStorage.removeItem(sessionKey);
        const renewed = {...response, token: 'newer-token'};
        session.set(renewed, true);

        notifyStorage(sessionKey);

        expect(session.user()).toEqual(renewed.userDto);
        expect(session.token()).toBe(renewed.token);
    });

    it('preserves a tab-only session when another tab removes a remembered session', () => {
        const session = TestBed.inject(AuthSession);
        session.set(response);
        notifyStorage(sessionKey);
        notifyStorage(null);

        expect(session.user()).toEqual(response.userDto);
        expect(session.token()).toBe(response.token);
        expect(sessionStorage.getItem(sessionKey)).toBe(JSON.stringify(response));
    });

    it('ignores unrelated keys and session-storage events', () => {
        const session = TestBed.inject(AuthSession);
        session.set(response, true);
        localStorage.removeItem(sessionKey);

        notifyStorage('theme');
        window.dispatchEvent(new StorageEvent('storage', {key: sessionKey, storageArea: sessionStorage}));

        expect(session.user()).toEqual(response.userDto);
        notifyStorage(sessionKey);
        expect(session.user()).toBeNull();
    });

    it.each(['quiz-history', 'attempts/attempt-id/progress'])(
        'does not send a cached bearer token to %s while the logout event is queued', (route) => {
            TestBed.configureTestingModule({
                providers: [
                    provideHttpClient(withInterceptors([authInterceptor])),
                    provideHttpClientTesting(),
                ]
            });
            const session = TestBed.inject(AuthSession);
            const http = TestBed.inject(HttpClient);
            const controller = TestBed.inject(HttpTestingController);
            const base = TestBed.inject(API_CONFIG).baseUrl;
            session.set(response, true);
            localStorage.removeItem(sessionKey);

            http.get(`${base}/${route}`).subscribe();
            const request = controller.expectOne(`${base}/${route}`);
            request.flush([]);

            expect(request.request.headers.has('Authorization')).toBe(false);
            expect(session.user()).toBeNull();
            controller.verify();
        });

    it('retains memory fallback when storing a remembered login fails', () => {
        vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
            throw new Error('Storage unavailable');
        });
        const session = TestBed.inject(AuthSession);
        session.set(response, true);

        expect(session.token()).toBe(response.token);
    });

    it('removes its storage listener when the service is destroyed', () => {
        const addListener = vi.spyOn(window, 'addEventListener');
        const removeListener = vi.spyOn(window, 'removeEventListener');
        TestBed.inject(AuthSession);
        const registration = addListener.mock.calls.find(([event]) => event === 'storage');
        expect(registration).toBeDefined();

        TestBed.resetTestingModule();

        expect(removeListener).toHaveBeenCalledWith('storage', registration![1]);
    });
});

function notifyStorage(key: string | null, newValue: string | null = null): void {
    window.dispatchEvent(new StorageEvent('storage', {key, newValue, storageArea: localStorage}));
}
