import {computed, DestroyRef, inject, Injectable, signal} from '@angular/core';

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
    private rememberedValue: string | null = null;
    private readonly session = signal<AuthResponse | null>(this.restore());
    readonly user = computed(() => this.session()?.userDto ?? null);

    constructor() {
        const onStorage = (event: StorageEvent): void => {
            try {
                if ((event.key === SESSION_KEY || event.key === null) && event.storageArea === localStorage) {
                    this.synchronizeRememberedSession();
                }
            } catch {
                /* Memory session works when storage is unavailable. */
            }
        };
        window.addEventListener('storage', onStorage);
        inject(DestroyRef).onDestroy(() => window.removeEventListener('storage', onStorage));
    }

    token(): string | null {
        this.synchronizeRememberedSession();
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
            throw new Error('Invalid login response.');
        }
        this.clear();
        this.session.set(response);
        try {
            const storage = remember ? localStorage : sessionStorage;
            const raw = JSON.stringify(response);
            storage.setItem(SESSION_KEY, raw);
            this.rememberedValue = remember ? raw : null;
        } catch {
            /* Memory session works when storage is unavailable. */
        }
    }

    clear(): void {
        this.rememberedValue = null;
        this.session.set(null);
        for (const kind of ['sessionStorage', 'localStorage'] as const) {
            try {
                globalThis[kind].removeItem(SESSION_KEY);
            } catch {
                /* Continue clearing the other storage when one is unavailable. */
            }
        }
    }

    private synchronizeRememberedSession(): void {
        if (this.rememberedValue === null) {
            return;
        }
        try {
            if (localStorage.getItem(SESSION_KEY) === this.rememberedValue) {
                return;
            }
        } catch {
            return;
        }
        this.rememberedValue = null;
        this.session.set(null);
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
                if (kind === 'localStorage') {
                    this.rememberedValue = raw;
                }
                return value;
            }

            storage.removeItem(SESSION_KEY);
            return null;
        } catch {
            return null;
        }
    }
}
