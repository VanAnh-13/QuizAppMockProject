import {HttpErrorResponse} from '@angular/common/http';
import {TestBed} from '@angular/core/testing';
import {ContactApi} from '../application/contact-api';
import {ContactStore} from './contact.store';

describe('ContactStore', () => {
    const receipt = {id: 'feedback-123', receivedAt: '2026-09-20T00:00:00Z', confirmationEmailSent: true};

    function setup(send = vi.fn().mockResolvedValue(receipt)) {
        TestBed.configureTestingModule({
            providers: [ContactStore, {provide: ContactApi, useValue: {send}}],
        });

        return {store: TestBed.inject(ContactStore), send};
    }

    function fill(store: ContactStore): void {
        store.form.setValue({
            fullName: 'Nguyen Van An',
            email: 'an@example.com',
            subject: 'Quiz feedback',
            message: 'The Angular quiz helped me prepare for the exam.',
        });
    }

    it('does not call the API when the form is invalid', async () => {
        const {store, send} = setup();

        expect(await store.submit()).toBe(false);
        expect(send).not.toHaveBeenCalled();
        expect(store.submitted()).toBe(false);
        expect(store.fieldError('fullName')).toBe('Full name is required.');
    });

    it('sends the message and clears the form on success', async () => {
        const {store, send} = setup();
        fill(store);

        expect(await store.submit()).toBe(true);
        expect(send).toHaveBeenCalledExactlyOnceWith({
            fullName: 'Nguyen Van An',
            email: 'an@example.com',
            subject: 'Quiz feedback',
            message: 'The Angular quiz helped me prepare for the exam.',
        });
        expect(store.submitted()).toBe(true);
        expect(store.error()).toBeNull();
        expect(store.form.value.fullName).toBe('');
    });

    it('keeps the form and shows an error when send fails', async () => {
        const {store} = setup(vi.fn().mockRejectedValue(new HttpErrorResponse({status: 500})));
        fill(store);

        expect(await store.submit()).toBe(false);
        expect(store.submitted()).toBe(false);
        expect(store.error()).toBe('Could not send your message. Try again.');
        expect(store.form.value.email).toBe('an@example.com');
    });

    it('accepts saved feedback even when confirmation email fails', async () => {
        const saved = {...receipt, confirmationEmailSent: false};
        const {store, send} = setup(vi.fn().mockResolvedValue(saved));
        fill(store);

        expect(await store.submit()).toBe(true);
        expect(store.receipt()).toEqual(saved);
        expect(store.submitted()).toBe(true);
        expect(store.error()).toBeNull();
        expect(store.form.controls.message.value).toBe('');
        expect(send).toHaveBeenCalledTimes(1);
    });
});
