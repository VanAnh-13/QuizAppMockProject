import {computed, inject, Injectable, signal} from '@angular/core';
import {FormControl, FormGroup, Validators} from '@angular/forms';
import {apiErrorMessage} from '../../../core/api/api-error';
import {ContactApi, ContactReceipt} from '../application/contact-api';

const FULL_NAME_MAX = 200;
const EMAIL_MAX = 256;
const SUBJECT_MAX = 200;
const MESSAGE_MAX = 2000;

@Injectable()
export class ContactStore {
    private readonly api = inject(ContactApi);

    readonly form = new FormGroup({
        fullName: new FormControl('', {
            nonNullable: true,
            validators: [Validators.required, Validators.maxLength(FULL_NAME_MAX)],
        }),
        email: new FormControl('', {
            nonNullable: true,
            validators: [Validators.required, Validators.email, Validators.maxLength(EMAIL_MAX)],
        }),
        subject: new FormControl('', {
            nonNullable: true,
            validators: [Validators.maxLength(SUBJECT_MAX)],
        }),
        message: new FormControl('', {
            nonNullable: true,
            validators: [Validators.required, Validators.maxLength(MESSAGE_MAX)],
        }),
    });
    readonly busy = signal(false);
    readonly error = signal<string | null>(null);
    readonly receipt = signal<ContactReceipt | null>(null);
    readonly submitted = computed(() => this.receipt() !== null);

    fieldError(name: 'fullName' | 'email' | 'message'): string | null {
        const control = this.form.controls[name];
        if (!control.touched && !control.dirty) {
            return null;
        }
        if (control.hasError('required')) {
            return name === 'fullName'
                ? 'Full name is required.'
                : name === 'email'
                    ? 'Email is required.'
                    : 'Message is required.';
        }
        if (control.hasError('email')) {
            return 'Enter a valid email address.';
        }
        if (control.hasError('maxlength')) {
            return `At most ${control.getError('maxlength').requiredLength} characters.`;
        }
        return null;
    }

    async submit(): Promise<boolean> {
        if (this.busy()) return false;

        this.form.markAllAsTouched();
        this.error.set(null);
        this.receipt.set(null);

        if (this.form.invalid) return false;

        this.busy.set(true);
        const value = this.form.getRawValue();

        try {
            const receipt = await this.api.send({
                fullName: value.fullName.trim(),
                email: value.email.trim(),
                subject: value.subject.trim(),
                message: value.message.trim(),
            });
            this.receipt.set(receipt);
            this.form.reset();
            return true;
        } catch (error) {
            this.error.set(apiErrorMessage(error, 'Could not send your message. Try again.'));
            return false;
        } finally {
            this.busy.set(false);
        }
    }
}
