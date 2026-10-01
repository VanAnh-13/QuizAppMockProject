import {ChangeDetectionStrategy, ChangeDetectorRef, Component, DestroyRef, ElementRef, inject} from '@angular/core';
import {Location} from '@angular/common';
import {ReactiveFormsModule} from '@angular/forms';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {SiteHeaderComponent} from '../../../shared/ui/site-header/site-header.component';
import {PasswordRecoveryStore} from './password-recovery.store';

@Component({
    selector: 'app-password-recovery-page',
    imports: [ReactiveFormsModule, RouterLink, SiteHeaderComponent],
    providers: [PasswordRecoveryStore],
    changeDetection: ChangeDetectionStrategy.OnPush,
    templateUrl: './password-recovery.page.html',
    styleUrl: './password-recovery.page.css',
})
export class PasswordRecoveryPage {
    protected readonly store = inject(PasswordRecoveryStore);
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly location = inject(Location);
    private readonly element = inject<ElementRef<HTMLElement>>(ElementRef);
    private readonly changeDetector = inject(ChangeDetectorRef);
    private readonly destroyRef = inject(DestroyRef);
    protected readonly resetting = this.route.snapshot.data['resetting'] === true;
    protected readonly fields = this.resetting ? [
        {name: 'newPassword', label: 'New password'},
        {name: 'confirmNewPassword', label: 'Confirm new password'},
    ] as const : [{name: 'email', label: 'Email address'}] as const;

    constructor() {
        this.store.configure(this.resetting, this.route.snapshot.fragment);
    }

    protected async submit(): Promise<void> {
        const succeeded = await this.store.submit();
        if (this.destroyRef.destroyed) return;
        if (succeeded && this.resetting) {
            this.location.replaceState('/reset-password');
            await this.router.navigate(['/login'], {replaceUrl: true, state: {passwordReset: true}});
            return;
        }
        this.changeDetector.detectChanges();
        this.element.nativeElement.querySelector<HTMLElement>(
            succeeded ? '[role="status"]' : '[aria-invalid="true"], [role="alert"]',
        )?.focus();
    }
}
