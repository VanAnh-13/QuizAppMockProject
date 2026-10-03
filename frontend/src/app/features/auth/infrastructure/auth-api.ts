import {inject, Injectable} from '@angular/core';
import {ApiClient} from '../../../core/api/api-client';
import {AuthResponse} from '../../../core/auth/auth-session';
import {ForgotPasswordRequest, LoginRequest, RegisterRequest, ResetPasswordRequest, UserDto} from '../domain/auth-contracts';

@Injectable({providedIn: 'root'})
export class AuthApi {
    private readonly api = inject(ApiClient);

    forgotPassword(request: ForgotPasswordRequest): Promise<{ readonly message: string }> {
        return this.api.post('auth/forgot-password', request);
    }

    resetPassword(request: ResetPasswordRequest): Promise<void> {
        return this.api.post<void>('auth/reset-password', request);
    }

    login(request: LoginRequest): Promise<AuthResponse> {
        return this.api.post<AuthResponse>('auth/login', request);
    }

    register(request: RegisterRequest): Promise<UserDto> {
        return this.api.post<UserDto>('auth/register', request);
    }
}
