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
                apiErrorMessage(error, 'Không thể tải thông tin hồ sơ. Vui lòng thử lại.'),
            );
        } finally {
            if (version === this.loadVersion) this.isLoading.set(false);
        }
    }

    fieldError(name: keyof AccountSettingsStore['form']['controls']): string | null {
        const control = this.form.controls[name];

        if (!control.touched) return null;

        if (control.hasError('required')) return 'Vui lòng điền thông tin này.';

        if (control.hasError('minlength'))
            return `Mật khẩu cần ít nhất ${AUTH_LIMITS.passwordMin} ký tự.`;

        if (control.hasError('maxlength'))
            return `Tối đa ${control.getError('maxlength').requiredLength} ký tự.`;

        if (name === 'newPassword' && this.form.hasError('passwordReused'))
            return 'Mật khẩu mới phải khác mật khẩu hiện tại.';

        if (name === 'confirmNewPassword' && this.form.hasError('passwordMismatch'))
            return 'Mật khẩu xác nhận chưa khớp.';

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
                    ? 'Mật khẩu hiện tại chưa đúng, hoặc phiên đăng nhập đã kết thúc. Vui lòng đăng nhập lại và thử lại.'
                    : apiErrorMessage(error, 'Không thể đổi mật khẩu. Vui lòng kiểm tra thông tin và thử lại.'),
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
