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

        if (!session) {
            return null;
        }

        if (Date.parse(session.expiresAt) <= Date.now()) {
            this.clear();
            return null;
        }

        return session.token;
    }

    set(response: AuthResponse, remember = false): void {
        if (
            !response.token ||
            !response.userDto?.id ||
            !Number.isFinite(Date.parse(response.expiresAt))
        ) {
            throw new Error('Phản hồi đăng nhập không hợp lệ.');
        }
        this.clear();
        this.session.set(response);
        try {
            const storage = remember ? localStorage : sessionStorage;
            storage.setItem(SESSION_KEY, JSON.stringify(response));
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
        try {
            localStorage.removeItem(SESSION_KEY);
        } catch {
            /* Memory logout still works when storage is unavailable. */
        }
    }

    private restore(): AuthResponse | null {
        return this.readStoredSession('sessionStorage') ?? this.readStoredSession('localStorage');
    }

    private readStoredSession(kind: 'sessionStorage' | 'localStorage'): AuthResponse | null {
        try {
            const storage = globalThis[kind];
            const raw = storage.getItem(SESSION_KEY);

            if (!raw) {
                return null;
            }

            const value = JSON.parse(raw) as AuthResponse;

            if (
                typeof value.token === 'string' &&
                value.userDto?.id &&
                Date.parse(value.expiresAt) > Date.now()
            ) {
                return value;
            }

            try {
                storage.removeItem(SESSION_KEY);
            } catch {
                /* Best-effort cleanup when storage is restricted. */
            }
            return null;
        } catch {
            return null;
        }
    }
}
