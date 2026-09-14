import {inject, Injectable} from '@angular/core';
import {ApiClient} from '../../../core/api/api-client';
import {AuthResponse} from '../../../core/auth/auth-session';
import {LoginRequest, RegisterRequest, UserDto} from '../domain/auth-contracts';

@Injectable({providedIn: 'root'})
export class AuthApi {
    private readonly api = inject(ApiClient);

    login(request: LoginRequest): Promise<AuthResponse> {
        return this.api.post<AuthResponse>('auth/login', request);
    }

    register(request: RegisterRequest): Promise<UserDto> {
        return this.api.post<UserDto>('auth/register', request);
    }
}
