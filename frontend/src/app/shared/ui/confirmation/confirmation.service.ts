import {Injectable, signal} from '@angular/core';

export interface ConfirmationRequest {
    readonly title: string;
    readonly message: string;
    readonly confirmLabel: string;
    readonly cancelLabel: string;
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
    }): Promise<boolean> {
        if (this.pending) {
            this.pending.resolve(false);
        }

        return new Promise<boolean>((resolve) => {
            const confirmation: PendingConfirmation = {
                title: options.title,
                message: options.message,
                confirmLabel: options.confirmLabel ?? 'Xác nhận',
                cancelLabel: options.cancelLabel ?? 'Hủy',
                resolve,
            };

            this.pending = confirmation;
            this.request.set(confirmation);
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
