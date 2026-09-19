import {Injectable, signal} from '@angular/core';

export type ConfirmationVariant = 'primary' | 'danger' | 'success' | 'warning';
export type ConfirmationMode = 'confirm' | 'notify';

export interface ConfirmationRequest {
    readonly title: string;
    readonly message: string;
    readonly confirmLabel: string;
    readonly cancelLabel?: string;
    readonly variant?: ConfirmationVariant;
    readonly mode?: ConfirmationMode;
    readonly icon?: string;
}

interface PendingConfirmation extends ConfirmationRequest {
    readonly resolve: (result: boolean) => void;
}

@Injectable({providedIn: 'root'})
export class ConfirmationService {
    readonly request = signal<ConfirmationRequest | null>(null);
    private pending: PendingConfirmation | null = null;

    confirm(options: {
        title: string;
        message: string;
        confirmLabel?: string;
        cancelLabel?: string;
        variant?: ConfirmationVariant;
        icon?: string;
    }): Promise<boolean> {
        if (this.pending) {
            this.pending.resolve(false);
        }

        return new Promise<boolean>((resolve) => {
            const request: ConfirmationRequest = {
                title: options.title,
                message: options.message,
                confirmLabel: options.confirmLabel ?? 'Confirm',
                cancelLabel: options.cancelLabel ?? 'Cancel',
                ...(options.variant !== undefined ? {variant: options.variant} : {}),
                ...(options.icon !== undefined ? {icon: options.icon} : {}),
            };

            this.pending = {
                ...request,
                resolve,
            };
            this.request.set(request);
        });
    }

    notify(options: {
        title: string;
        message: string;
        confirmLabel?: string;
        variant?: ConfirmationVariant;
        icon?: string;
    }): Promise<void> {
        if (this.pending) {
            this.pending.resolve(false);
        }

        const variant = options.variant ?? 'success';
        const defaultIcon = variant === 'success' ? 'check_circle' : (variant === 'danger' ? 'error' : 'info');

        return new Promise<void>((resolve) => {
            const request: ConfirmationRequest = {
                title: options.title,
                message: options.message,
                confirmLabel: options.confirmLabel ?? 'OK',
                variant,
                mode: 'notify',
                icon: options.icon ?? defaultIcon,
            };

            this.pending = {
                ...request,
                resolve: () => resolve(),
            };
            this.request.set(request);
        });
    }

    accept(): void {
        const pending = this.pending;
        this.pending = null;
        this.request.set(null);
        pending?.resolve(true);
    }

    cancel(): void {
        const pending = this.pending;
        this.pending = null;
        this.request.set(null);
        pending?.resolve(false);
    }
}
