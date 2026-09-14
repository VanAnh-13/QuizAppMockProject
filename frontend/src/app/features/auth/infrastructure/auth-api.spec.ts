import {provideHttpClient} from '@angular/common/http';
import {HttpTestingController, provideHttpClientTesting} from '@angular/common/http/testing';
import {TestBed} from '@angular/core/testing';
import {API_CONFIG} from '../../../core/config/api.config';
import {UserDto} from '../domain/auth-contracts';
import {AuthApi} from './auth-api';

describe('AuthApi HTTP contracts', () => {
    beforeEach(() => TestBed.configureTestingModule({providers: [provideHttpClient(), provideHttpClientTesting()]}));
    afterEach(() => TestBed.inject(HttpTestingController).verify());

    it('posts credentials through the configured API client and returns the auth response', async () => {
        const api = TestBed.inject(AuthApi);
        const request = {username: 'learner', password: 'Password-123!'};
        const result = api.login(request);
        const http = TestBed.inject(HttpTestingController).expectOne(`${TestBed.inject(API_CONFIG).baseUrl}/auth/login`);
        expect(http.request.method).toBe('POST');
        expect(http.request.body).toEqual(request);
        const response = {
            token: 'test-token',
            expiresAt: '2030-01-01T00:00:00Z',
            userDto: {id: 'learner-id', username: 'learner', fullName: 'Quiz Learner'}
        };
        http.flush(response);
        expect(await result).toEqual(response);
    });

    it('preserves the complete profile and propagates registration conflicts', async () => {
        const request = {
            username: 'learner',
            password: 'Password-123!',
            confirmPassword: 'Password-123!',
            email: 'learner@example.com',
            profile: {fullName: 'Quiz Learner', phoneNumber: null, dateOfBirth: '2000-01-02'}
        };
        const result = TestBed.inject(AuthApi).register(request);
        const rejected = expect(result).rejects.toMatchObject({status: 409});
        const http = TestBed.inject(HttpTestingController).expectOne(`${TestBed.inject(API_CONFIG).baseUrl}/auth/register`);
        expect(http.request.method).toBe('POST');
        expect(http.request.body).toEqual(request);
        http.flush({}, {status: 409, statusText: 'Conflict'});
        await rejected;
    });

    it('resolves the created user account returned by registration', async () => {
        const request = {
            username: 'learner',
            password: 'Password-123!',
            confirmPassword: 'Password-123!',
            email: 'learner@example.com',
            profile: {fullName: 'Quiz Learner', phoneNumber: null, dateOfBirth: '2000-01-02'}
        };
        const created: UserDto = {
            id: 'learner-id',
            username: 'learner',
            email: 'learner@example.com',
            fullName: 'Quiz Learner',
            phoneNumber: null,
            dateOfBirth: '2000-01-02',
            avatar: null,
            isActive: true,
            createdAt: '2026-09-14T08:00:00Z',
            updatedAt: '2026-09-14T08:00:00Z',
            roles: [{id: 'student-role', roleName: 'Student', description: null, isActive: true}],
        };
        const result = TestBed.inject(AuthApi).register(request);
        const http = TestBed.inject(HttpTestingController).expectOne(`${TestBed.inject(API_CONFIG).baseUrl}/auth/register`);
        expect(http.request.method).toBe('POST');
        expect(http.request.body).toEqual(request);
        http.flush(created, {status: 201, statusText: 'Created'});
        const registered: UserDto = await result;
        expect(registered).toEqual(created);
    });
});
