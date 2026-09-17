import {Injectable, signal} from '@angular/core';

export interface ErrorNotification {
    readonly id: string;
    readonly message: string;
}

@Injectable({providedIn: 'root'})
export class ErrorNotificationService {
    readonly notifications = signal<readonly ErrorNotification[]>([]);
    private counter = 0;

    show(message: string, options: { duration?: number } = {}): void {
        const id = `error-${++this.counter}`;
        const duration = options.duration ?? 5000;

        this.notifications.update((list) => [...list, {id, message}]);

        setTimeout(() => this.dismiss(id), duration);
    }

    dismiss(id: string): void {
        this.notifications.update((list) => list.filter((n) => n.id !== id));
    }
}
