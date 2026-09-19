import {ChangeDetectionStrategy, Component, signal} from '@angular/core';
import {FormControl, FormGroup, ReactiveFormsModule, Validators} from '@angular/forms';
import {SiteFooterComponent} from '../../../shared/ui/site-footer/site-footer.component';
import {SiteHeaderComponent} from '../../../shared/ui/site-header/site-header.component';

const FAQS = [
    {
        question: 'Do I need to sign in to take a quiz?',
        answer: 'You can browse sample quizzes without an account. Signing in lets you save progress, review scores, and keep your attempt history.',
    },
    {
        question: 'How do I reset my password?',
        answer: 'Automatic password reset is not available in this version. If you forget your password, contact the administrator who provided your account.',
    },
    {
        question: 'Are quizzes timed?',
        answer: 'Each published quiz uses the time limit stored with that quiz. Saved answers remain available if you pause and resume the same attempt.',
    },
] as const;

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [ReactiveFormsModule, SiteFooterComponent, SiteHeaderComponent],
    selector: 'app-contact-page',
    styleUrl: './contact.page.css',
    templateUrl: './contact.page.html',
})
export class ContactPage {
    readonly form = new FormGroup({
        fullName: new FormControl('', {nonNullable: true, validators: [Validators.required]}),
        email: new FormControl('', {
            nonNullable: true,
            validators: [Validators.required, Validators.email],
        }),
        subject: new FormControl('', {nonNullable: true}),
        message: new FormControl('', {nonNullable: true, validators: [Validators.required]}),
    });
    protected readonly submitted = signal(false);
    protected readonly faqs = FAQS;

    protected fieldError(name: 'fullName' | 'email' | 'message'): string | null {
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
        return null;
    }

    protected submit(): void {
        this.form.markAllAsTouched();
        if (this.form.invalid) {
            this.submitted.set(false);
            return;
        }
        this.submitted.set(true);
        this.form.reset();
    }
}
