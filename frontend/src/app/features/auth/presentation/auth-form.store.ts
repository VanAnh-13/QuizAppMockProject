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
                ? 'Please accept the terms before signing up.'
                : 'Please complete this field.';

        if (control.hasError('email'))
            return 'Enter a valid email address.';

        if (control.hasError('minlength'))
            return `Password must contain at least ${AUTH_LIMITS.passwordMin} characters.`;

        if (control.hasError('maxlength'))
            return `Maximum ${control.getError('maxlength').requiredLength} characters.`;

        if (control.hasError('date'))
            return 'Enter a valid date of birth that is not in the future.';

        if (name === 'confirmPassword' && this.form.hasError('passwordMismatch'))
            return 'Passwords do not match.';

        if (name === 'givenName' && this.form.hasError('fullNameLength'))
            return `Full name must not exceed ${AUTH_LIMITS.fullName} characters.`;

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
                ? 'Incorrect username or password.'
                : apiErrorMessage(error, 'Could not process your request. Check your details and try again.', {
                    conflictMessage: 'This username or email is already in use. Choose another or log in.',
                }));
            return false;
        } finally {
            this.form.controls.password.reset();
            this.form.controls.confirmPassword.reset();
            this.busy.set(false);
        }
    }
}
