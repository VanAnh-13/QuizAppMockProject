import {inject, Injectable} from '@angular/core';
import {ApiClient} from '../../../core/api/api-client';

export interface ContactMessage {
    readonly fullName: string;
    readonly email: string;
    readonly subject: string;
    readonly message: string;
}

export interface ContactReceipt {
    readonly id: string;
    readonly receivedAt: string;
    readonly confirmationEmailSent: boolean;
}

@Injectable({providedIn: 'root'})
export class ContactApi {
    private readonly api = inject(ApiClient);

    send(message: ContactMessage): Promise<ContactReceipt> {
        return this.api.post<ContactReceipt>('public/contact', message);
    }
}
