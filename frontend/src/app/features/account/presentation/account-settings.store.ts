import {HttpErrorResponse} from '@angular/common/http';
import {computed, inject, Injectable, signal} from '@angular/core';
import {FormControl, FormGroup, ValidationErrors, Validators} from '@angular/forms';
import {apiErrorMessage} from '../../../core/api/api-error';
import {AUTH_LIMITS} from '../../auth/domain/auth-contracts';
import {ACCOUNT_API} from '../application/account-api';
import {AccountProfile, accountInitials} from '../domain/account-contracts';

@Injectable()
export class AccountSettingsStore {
    private readonly api = inject(ACCOUNT_API);
    private loadVersion = 0;

    readonly profile = signal<AccountProfile | null>(null);
    readonly activeRoles = computed(() => this.profile()?.roles.filter((role) => role.isActive) ?? []);
    readonly isLoading = signal(true);
    readonly loadError = signal<string | null>(null);
    readonly busy = signal(false);
    readonly error = signal<string | null>(null);
    readonly succeeded = signal(false);
    readonly requiresSignIn = signal(false);

    readonly initials = computed(() => {
        const profile = this.profile();

        return profile ? accountInitials(profile) : '';
    });

    readonly form = new FormGroup({
        currentPassword: new FormControl('', {
            nonNullable: true,
            validators: [Validators.required, Validators.maxLength(AUTH_LIMITS.passwordMax)],
        }),
        newPassword: new FormControl('', {
            nonNullable: true,
            validators: [
                Validators.required,
                Validators.minLength(AUTH_LIMITS.passwordMin),
                Validators.maxLength(AUTH_LIMITS.passwordMax),
            ],
        }),
        confirmNewPassword: new FormControl('', {
            nonNullable: true,
            validators: [Validators.required],
        }),
    });

    constructor() {
        this.form.setValidators((control) => {
            const {currentPassword, newPassword, confirmNewPassword} = control.getRawValue();
            const errors: ValidationErrors = {};

            if (newPassword && currentPassword && newPassword === currentPassword)
                errors['passwordReused'] = true;

            if (confirmNewPassword && newPassword !== confirmNewPassword)
                errors['passwordMismatch'] = true;

            return Object.keys(errors).length ? errors : null;
        });
        void this.load();
    }

    async load(): Promise<void> {
        const version = ++this.loadVersion;
        this.isLoading.set(true);
        this.loadError.set(null);

        try {
            const profile = await this.api.profile();
            if (version !== this.loadVersion) return;
            this.profile.set(profile);
        } catch (error) {
            if (version !== this.loadVersion) return;
            this.profile.set(null);
            this.loadError.set(
                apiErrorMessage(error, 'Could not load your profile details. Please try again.'),
            );
        } finally {
            if (version === this.loadVersion) this.isLoading.set(false);
        }
    }

    fieldError(name: keyof AccountSettingsStore['form']['controls']): string | null {
        const control = this.form.controls[name];

        if (!control.touched) return null;

        if (control.hasError('required')) return 'This field is required.';

        if (control.hasError('minlength'))
            return `Password must be at least ${AUTH_LIMITS.passwordMin} characters.`;

        if (control.hasError('maxlength'))
            return `Maximum ${control.getError('maxlength').requiredLength} characters.`;

        if (name === 'newPassword' && this.form.hasError('passwordReused'))
            return 'New password must be different from current password.';

        if (name === 'confirmNewPassword' && this.form.hasError('passwordMismatch'))
            return 'Confirmation password does not match.';

        return null;
    }

    async submit(): Promise<boolean> {
        if (this.busy()) return false;

        this.form.markAllAsTouched();
        this.error.set(null);
        this.succeeded.set(false);

        if (this.form.invalid) return false;

        this.busy.set(true);
        const value = this.form.getRawValue();

        try {
            await this.api.changePassword({
                currentPassword: value.currentPassword,
                newPassword: value.newPassword,
                confirmNewPassword: value.confirmNewPassword,
            });
            this.succeeded.set(true);
            this.requiresSignIn.set(true);
            this.form.reset();

            return true;
        } catch (error) {
            const unauthorized = error instanceof HttpErrorResponse && error.status === 401;

            if (unauthorized) this.requiresSignIn.set(true);

            this.error.set(
                unauthorized
                    ? 'Current password is incorrect or your session has expired. Please log in again.'
                    : apiErrorMessage(error, 'Could not change password. Please check your information and try again.'),
            );

            return false;
        } finally {
            this.form.controls.currentPassword.reset();
            this.form.controls.newPassword.reset();
            this.form.controls.confirmNewPassword.reset();
            this.busy.set(false);
        }
    }
}
