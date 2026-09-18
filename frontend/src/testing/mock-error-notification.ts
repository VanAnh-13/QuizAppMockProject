import {Injectable, signal} from '@angular/core';
import {ErrorNotification} from '../app/core/errors/error-notification.service';

@Injectable()
export class MockErrorNotificationService {
    readonly notifications = signal<readonly ErrorNotification[]>([]);
    readonly messages: string[] = [];
    private counter = 0;

    show(message: string, _options: { duration?: number } = {}): void {
        this.messages.push(message);
        const item: ErrorNotification = {
            id: `mock-error-${++this.counter}`,
            message,
        };
        this.notifications.set([...this.notifications(), item]);
    }

    dismiss(id: string): void {
        this.notifications.set(this.notifications().filter((n) => n.id !== id));
    }

    clear(): void {
        this.messages.length = 0;
        this.notifications.set([]);
    }

    hasMessage(substring: string): boolean {
        return this.messages.some((m) => m.includes(substring));
    }
}
