import {HttpErrorResponse} from '@angular/common/http';
import {inject, Injectable, signal} from '@angular/core';
import {AbstractControl, FormControl, FormGroup, ValidationErrors, Validators} from '@angular/forms';
import {apiErrorMessage} from '../../../core/api/api-error';
import {AuthSession} from '../../../core/auth/auth-session';
import {AUTH_LIMITS} from '../domain/auth-contracts';
import {AuthApi} from '../infrastructure/auth-api';

const requiredText = (control: AbstractControl): ValidationErrors | null =>
    typeof control.value === 'string' && control.value.trim() ? null : {required: true};

@Injectable()
export class AuthFormStore {
    private readonly api = inject(AuthApi);
    private readonly session = inject(AuthSession);
    private registering = false;
    readonly busy = signal(false);
    readonly error = signal<string | null>(null);
    readonly today = new Date().toISOString().slice(0, 10);
    readonly form = new FormGroup({
        familyName: new FormControl('', {nonNullable: true}),
        givenName: new FormControl('', {nonNullable: true}),
        email: new FormControl('', {nonNullable: true}),
        username: new FormControl('', {
            nonNullable: true,
            validators: [requiredText, Validators.maxLength(AUTH_LIMITS.username)],
        }),
        phoneNumber: new FormControl('', {nonNullable: true}),
        dateOfBirth: new FormControl('', {nonNullable: true}),
        password: new FormControl('', {nonNullable: true, validators: [Validators.required]}),
        confirmPassword: new FormControl('', {nonNullable: true}),
        remember: new FormControl(false, {nonNullable: true}),
        terms: new FormControl(false, {nonNullable: true}),
    });

    configure(registering: boolean): void {
        this.registering = registering;

        if (!registering) {
            return;
        }

        const controls = this.form.controls;

        controls.familyName.setValidators([requiredText]);
        controls.givenName.setValidators([requiredText]);
        controls.email.setValidators([requiredText, Validators.email, Validators.maxLength(AUTH_LIMITS.email)]);
        controls.phoneNumber.setValidators([Validators.maxLength(AUTH_LIMITS.phoneNumber)]);
        controls.dateOfBirth.setValidators([(control) =>
            control.value && (!/^\d{4}-\d{2}-\d{2}$/.test(control.value) || control.value > this.today)
                ? {date: true} : null,
        ]);
        controls.password.setValidators([
            Validators.required,
            Validators.minLength(AUTH_LIMITS.passwordMin),
            Validators.maxLength(AUTH_LIMITS.passwordMax),
        ]);
        controls.confirmPassword.setValidators([Validators.required]);
        controls.terms.setValidators([Validators.requiredTrue]);
        this.form.setValidators((control) => {
            const {familyName, givenName, password, confirmPassword} = control.getRawValue();
            const errors: ValidationErrors = {};
            if (`${familyName.trim()} ${givenName.trim()}`.length > AUTH_LIMITS.fullName)
                errors['fullNameLength'] = true;

            if (confirmPassword && password !== confirmPassword)
                errors['passwordMismatch'] = true;

            return Object.keys(errors).length ? errors : null;
        });

        Object.values(controls).forEach((control) =>
            control.updateValueAndValidity());
    }

    fieldError(name: keyof AuthFormStore['form']['controls']): string | null {
        const control = this.form.controls[name];

        if (!control.touched)
            return null;

        if (control.hasError('required'))
            return name === 'terms'
                ? 'Vui lòng xác nhận trước khi đăng ký.'
                : 'Vui lòng điền thông tin này.';

        if (control.hasError('email'))
            return 'Địa chỉ email không hợp lệ.';

        if (control.hasError('minlength'))
            return `Mật khẩu cần ít nhất ${AUTH_LIMITS.passwordMin} ký tự.`;

        if (control.hasError('maxlength'))
            return `Tối đa ${control.getError('maxlength').requiredLength} ký tự.`;

        if (control.hasError('date'))
            return 'Ngày sinh phải hợp lệ và không ở tương lai.';

        if (name === 'confirmPassword' && this.form.hasError('passwordMismatch'))
            return 'Mật khẩu xác nhận chưa khớp.';

        if (name === 'givenName' && this.form.hasError('fullNameLength'))
            return `Họ và tên không quá ${AUTH_LIMITS.fullName} ký tự.`;

        return null;
    }

    async submit(): Promise<boolean> {
        if (this.busy())
            return false;

        this.form.markAllAsTouched();
        this.error.set(null);
        if (this.form.invalid)
            return false;


        this.busy.set(true);
        const value = this.form.getRawValue();
        try {
            if (this.registering) {
                await this.api.register({
                    username: value.username.trim(),
                    email: value.email.trim(),
                    password: value.password,
                    confirmPassword: value.confirmPassword,
                    profile: {
                        fullName: `${value.familyName.trim()} ${value.givenName.trim()}`,
                        phoneNumber: value.phoneNumber.trim() || null,
                        dateOfBirth: value.dateOfBirth || null,
                    },
                });
            } else {
                this.session.set(await this.api.login({
                    username: value.username.trim(),
                    password: value.password,
                }), value.remember);
            }
            return true;
        } catch (error) {
            this.error.set(error instanceof HttpErrorResponse && error.status === 401
                ? 'Tên đăng nhập hoặc mật khẩu chưa đúng.'
                : apiErrorMessage(error, 'Không thể xử lý yêu cầu. Vui lòng kiểm tra thông tin và thử lại.', {
                    conflictMessage: 'Tên đăng nhập hoặc email đã được sử dụng. Vui lòng chọn thông tin khác hoặc đăng nhập.',
                }));
            return false;
        } finally {
            this.form.controls.password.reset();
            this.form.controls.confirmPassword.reset();
            this.busy.set(false);
        }
    }
}
