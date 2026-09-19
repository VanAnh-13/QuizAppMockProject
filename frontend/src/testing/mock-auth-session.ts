import {computed, Injectable, signal} from '@angular/core';
import {AuthResponse} from '../app/core/auth/auth-session';
import {UserDto} from '../app/features/auth/domain/auth-contracts';
import {createTestUser} from './fixtures';

@Injectable()
export class MockAuthSession {
    readonly token = signal<string | null>(null);
    readonly user = signal<{
        readonly id: string;
        readonly username: string;
        readonly fullName: string | null
    } | null>(null);
    readonly isAuthenticated = computed(() => Boolean(this.token() && this.user()));

    set(session: AuthResponse): void {
        this.token.set(session.token);
        this.user.set(session.userDto);
    }

    clear(): void {
        this.token.set(null);
        this.user.set(null);
    }

    setAuthenticated(userOverrides: Partial<UserDto> = {}, token = 'mock-jwt-token'): void {
        const fullUser = createTestUser(userOverrides);
        this.token.set(token);
        this.user.set({
            id: fullUser.id,
            username: fullUser.username,
            fullName: fullUser.fullName,
        });
    }

    setAnonymous(): void {
        this.clear();
    }
}
