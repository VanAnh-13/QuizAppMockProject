import {ChangeDetectionStrategy, Component, inject, output, signal, viewChild,} from '@angular/core';
import {
    AbstractControl,
    FormControl,
    FormGroup,
    ReactiveFormsModule,
    ValidationErrors,
    Validators,
} from '@angular/forms';
import {apiErrorMessage} from '../../../core/api/api-error';
import {AuthSession} from '../../../core/auth/auth-session';
import {AuthApi} from '../../../features/auth/infrastructure/auth-api';
import {DialogComponent} from '../dialog/dialog.component';

@Component({
    selector: 'app-auth-dialog',
    imports: [DialogComponent, ReactiveFormsModule],
    changeDetection: ChangeDetectionStrategy.OnPush,
    templateUrl: './auth-dialog.component.html',
    styleUrl: './auth-dialog.component.css',
})
export class AuthDialogComponent {
    readonly authenticated = output<void>();
    private readonly api = inject(AuthApi);
    private readonly session = inject(AuthSession);
    private readonly dialog = viewChild.required<DialogComponent>('dialog');
    protected readonly registering = signal(false);
    protected readonly busy = signal(false);
    protected readonly error = signal<string | null>(null);
    protected readonly notice = signal<string | null>(null);
    protected readonly form = new FormGroup(
        {
            username: new FormControl('', {nonNullable: true, validators: [Validators.required]}),
            password: new FormControl('', {nonNullable: true, validators: [Validators.required]}),
            email: new FormControl('', {nonNullable: true}),
            fullName: new FormControl('', {nonNullable: true}),
            confirmPassword: new FormControl('', {nonNullable: true}),
        },
        {validators: passwordsMatch},
    );

    open(register = false): void {
        if (this.busy()) return;
        this.registering.set(register);
        this.form.reset();
        this.form.controls.email.setValidators(register ? [Validators.required, Validators.email] : []);
        this.form.controls.fullName.setValidators(register ? [Validators.required] : []);
        this.form.controls.confirmPassword.setValidators(register ? [Validators.required] : []);
        this.form.controls.email.updateValueAndValidity();
        this.form.controls.fullName.updateValueAndValidity();
        this.form.controls.confirmPassword.updateValueAndValidity();
        this.error.set(null);
        this.notice.set(null);
        this.dialog().open();
    }

    protected async submit(): Promise<void> {
        if (this.busy() || this.form.invalid) return;
        this.busy.set(true);
        this.error.set(null);
        const {username, password, email, fullName, confirmPassword} = this.form.getRawValue();
        try {
            if (this.registering()) {
                await this.api.register({
                    username,
                    email,
                    password,
                    confirmPassword,
                    profile: {fullName},
                });
                this.registering.set(false);
                this.form.controls.email.clearValidators();
                this.form.controls.fullName.clearValidators();
                this.form.controls.confirmPassword.clearValidators();
                this.form.controls.email.updateValueAndValidity();
                this.form.controls.fullName.updateValueAndValidity();
                this.form.controls.confirmPassword.updateValueAndValidity();
                this.notice.set('Tài khoản đã được tạo. Hãy đăng nhập để tiếp tục.');
            } else {
                this.session.set(await this.api.login({username, password}));
                this.dialog().close();
                this.authenticated.emit();
            }
        } catch (error) {
            this.error.set(
                apiErrorMessage(
                    error,
                    'Không thể đăng nhập hoặc tạo tài khoản. Vui lòng kiểm tra thông tin.',
                    {
                        conflictMessage: this.registering()
                            ? 'Tên đăng nhập hoặc email đã được sử dụng. Vui lòng chọn thông tin khác hoặc đăng nhập vào tài khoản hiện có.'
                            : undefined,
                    },
                ),
            );
        } finally {
            this.form.controls.password.reset();
            this.form.controls.confirmPassword.reset();
            this.busy.set(false);
        }
    }
}

function passwordsMatch(control: AbstractControl): ValidationErrors | null {
    const password = control.get('password')?.value;
    const confirmation = control.get('confirmPassword')?.value;
    return confirmation && password !== confirmation ? {passwordMismatch: true} : null;
}
