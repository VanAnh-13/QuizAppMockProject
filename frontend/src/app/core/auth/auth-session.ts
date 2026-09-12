import {computed, Injectable, signal} from '@angular/core';

export interface AuthResponse {
    readonly token: string;
    readonly expiresAt: string;
    readonly userDto: {
        readonly id: string;
        readonly username: string;
        readonly fullName: string | null;
    };
}

const SESSION_KEY = 'quizapp.session';

@Injectable({providedIn: 'root'})
export class AuthSession {
    private readonly session = signal<AuthResponse | null>(this.restore());
    readonly user = computed(() => this.session()?.userDto ?? null);

    token(): string | null {
        const session = this.session();
        if (!session || Date.parse(session.expiresAt) <= Date.now()) return null;
        return session.token;
    }

    set(response: AuthResponse): void {
        if (
            !response.token ||
            !response.userDto?.id ||
            !Number.isFinite(Date.parse(response.expiresAt))
        ) {
            throw new Error('Phản hồi đăng nhập không hợp lệ.');
        }
        this.session.set(response);
        try {
            sessionStorage.setItem(SESSION_KEY, JSON.stringify(response));
        } catch {
            /* Memory session works when storage is unavailable. */
        }
    }

    clear(): void {
        this.session.set(null);
        try {
            sessionStorage.removeItem(SESSION_KEY);
        } catch {
            /* No persistent session is available. */
        }
    }

    private restore(): AuthResponse | null {
        try {
            const raw = sessionStorage.getItem(SESSION_KEY);
            const value = raw ? (JSON.parse(raw) as AuthResponse) : null;
            return value &&
            typeof value.token === 'string' &&
            value.userDto?.id &&
            Date.parse(value.expiresAt) > Date.now()
                ? value
                : null;
        } catch {
            return null;
        }
    }
}
