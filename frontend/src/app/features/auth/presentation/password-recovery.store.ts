import {HttpErrorResponse} from '@angular/common/http';
import {inject, Injectable, signal} from '@angular/core';
import {NonNullableFormBuilder, Validators} from '@angular/forms';
import {apiErrorMessage} from '../../../core/api/api-error';
import {AuthSession} from '../../../core/auth/auth-session';
import {AUTH_LIMITS} from '../domain/auth-contracts';
import {AuthApi} from '../infrastructure/auth-api';

@Injectable()
export class PasswordRecoveryStore {
    private readonly api = inject(AuthApi);
    private readonly session = inject(AuthSession);
    private credentials: { userId: string; token: string } | null = null;
    private resetting = false;
    readonly busy = signal(false);
    readonly sent = signal(false);
    readonly invalidLink = signal(false);
    readonly error = signal<string | null>(null);
    readonly form = inject(NonNullableFormBuilder).group({
        email: ['', [Validators.required, Validators.email, Validators.maxLength(AUTH_LIMITS.email)]],
        newPassword: ['', [
            Validators.required,
            Validators.minLength(AUTH_LIMITS.passwordMin),
            Validators.maxLength(AUTH_LIMITS.passwordMax),
        ]],
        confirmNewPassword: ['', Validators.required],
    });

    configure(resetting: boolean, fragment: string | null): void {
        this.resetting = resetting;
        if (!resetting) {
            this.form.controls.newPassword.disable();
            this.form.controls.confirmNewPassword.disable();
            return;
        }
        this.form.controls.email.disable();
        this.form.setValidators((form) => {
            const {newPassword, confirmNewPassword} = form.getRawValue();
            return confirmNewPassword && newPassword !== confirmNewPassword ? {passwordMismatch: true} : null;
        });
        const parameters = new URLSearchParams(fragment ?? '');
        const userId = parameters.get('userId') ?? '';
        const token = parameters.get('token') ?? '';
        if (parameters.getAll('userId').length !== 1 || parameters.getAll('token').length !== 1
            || !/^[0-9a-f]{8}(-[0-9a-f]{4}){3}-[0-9a-f]{12}$/i.test(userId)
            || userId === '00000000-0000-0000-0000-000000000000'
            || !/^[A-Za-z0-9_-]{43}$/.test(token)) {
            this.invalidLink.set(true);
            return;
        }
        this.credentials = {userId, token};
    }

    fieldError(name: keyof PasswordRecoveryStore['form']['controls']): string | null {
        const control = this.form.controls[name];
        if (!control.touched) return null;
        if (control.hasError('required')) return 'Please complete this field.';
        if (control.hasError('email')) return 'Enter a valid email address.';
        if (control.hasError('minlength')) return `Password must contain at least ${AUTH_LIMITS.passwordMin} characters.`;
        if (control.hasError('maxlength')) return `Maximum ${control.getError('maxlength').requiredLength} characters.`;
        if (name === 'confirmNewPassword' && this.form.hasError('passwordMismatch')) return 'Passwords do not match.';
        return null;
    }

    requestAgain(): void {
        this.sent.set(false);
    }

    async submit(): Promise<boolean> {
        if (this.busy() || this.invalidLink()) return false;
        this.form.markAllAsTouched();
        this.error.set(null);
        if (this.form.invalid) return false;

        this.busy.set(true);
        try {
            const value = this.form.getRawValue();
            if (this.resetting && this.credentials) {
                await this.api.resetPassword({
                    ...this.credentials,
                    newPassword: value.newPassword,
                    confirmNewPassword: value.confirmNewPassword,
                });
                this.credentials = null;
                this.session.clear();
            } else {
                await this.api.forgotPassword({email: value.email.trim()});
                this.sent.set(true);
            }
            return true;
        } catch (error) {
            if (this.resetting && error instanceof HttpErrorResponse && error.status === 400) {
                this.invalidLink.set(true);
                this.credentials = null;
            } else {
                this.error.set(error instanceof HttpErrorResponse && error.status === 429
                    ? 'Too many requests. Please wait and try again later.'
                    : apiErrorMessage(error, 'Could not process your request. Please try again later.'));
            }
            return false;
        } finally {
            this.form.controls.newPassword.reset();
            this.form.controls.confirmNewPassword.reset();
            this.busy.set(false);
        }
    }
}
