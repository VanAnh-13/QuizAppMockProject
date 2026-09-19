import {Injectable, signal} from '@angular/core';
import {ConfirmationRequest} from '../app/shared/ui/confirmation/confirmation.service';

@Injectable()
export class MockConfirmationService {
    readonly request = signal<ConfirmationRequest | null>(null);
    autoResponse = true;
    lastOptions: { title: string; message: string; confirmLabel?: string; cancelLabel?: string } | null = null;

    confirm(options: {
        title: string;
        message: string;
        confirmLabel?: string;
        cancelLabel?: string;
        variant?: any;
        icon?: string;
    }): Promise<boolean> {
        this.lastOptions = options;
        this.request.set({
            title: options.title,
            message: options.message,
            confirmLabel: options.confirmLabel ?? 'Yes',
            cancelLabel: options.cancelLabel ?? 'No',
            variant: options.variant ?? 'primary',
            mode: 'confirm',
            icon: options.icon ?? 'help_outline',
        });
        return Promise.resolve(this.autoResponse);
    }

    notify(options: {
        title: string;
        message: string;
        confirmLabel?: string;
        variant?: any;
        icon?: string;
    }): Promise<void> {
        this.request.set({
            title: options.title,
            message: options.message,
            confirmLabel: options.confirmLabel ?? 'OK',
            variant: options.variant ?? 'success',
            mode: 'notify',
            icon: options.icon ?? 'check_circle',
        });
        return Promise.resolve();
    }

    accept(): void {
        this.request.set(null);
    }

    cancel(): void {
        this.request.set(null);
    }

    setAutoResponse(value: boolean): void {
        this.autoResponse = value;
    }
}
