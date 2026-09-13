import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { API_CONFIG } from '../config/api.config';
import { AuthResponse, AuthSession } from './auth-session';
import { authInterceptor } from './auth.interceptor';

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
    vi.useFakeTimers();
    vi.setSystemTime(signedInAt);
  });

  afterEach(() => {
    vi.restoreAllMocks();
    vi.useRealTimers();
    sessionStorage.clear();
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

    http.get(url).subscribe({ error: onError });

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
    request.flush({}, { status: 401, statusText: 'Unauthorized' });

    expect(onError).toHaveBeenCalledOnce();
    expect(session.token()).toBe(renewedSession.token);
    expect(session.user()).toEqual(renewedSession.userDto);
    controller.verify();
  });
});
