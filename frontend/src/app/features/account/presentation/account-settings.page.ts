import {
    ChangeDetectionStrategy,
    ChangeDetectorRef,
    Component,
    DestroyRef,
    ElementRef,
    inject,
    signal,
} from '@angular/core';
import {DatePipe} from '@angular/common';
import {ReactiveFormsModule} from '@angular/forms';
import {RouterLink} from '@angular/router';
import {AuthSession} from '../../../core/auth/auth-session';
import {SiteFooterComponent} from '../../../shared/ui/site-footer/site-footer.component';
import {SiteHeaderComponent} from '../../../shared/ui/site-header/site-header.component';
import {accountApiProvider} from '../infrastructure/account-api.provider';
import {AccountSettingsStore} from './account-settings.store';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [DatePipe, ReactiveFormsModule, RouterLink, SiteFooterComponent, SiteHeaderComponent],
    providers: [accountApiProvider, AccountSettingsStore],
    selector: 'app-account-settings-page',
    styleUrl: './account-settings.page.css',
    templateUrl: './account-settings.page.html',
})
export class AccountSettingsPage {
    protected readonly store = inject(AccountSettingsStore);
    protected readonly visiblePasswords = signal<ReadonlySet<string>>(new Set());
    private readonly session = inject(AuthSession);
    private readonly element = inject<ElementRef<HTMLElement>>(ElementRef);
    private readonly changeDetector = inject(ChangeDetectorRef);
    private destroyed = false;

    constructor() {
        inject(DestroyRef).onDestroy(() => {
            this.destroyed = true;
        });
    }

    protected togglePassword(name: string): void {
        this.visiblePasswords.update((visible) => {
            const next = new Set(visible);

            if (next.has(name)) {
                next.delete(name);
            } else {
                next.add(name);
            }

            return next;
        });
    }

    protected async submit(): Promise<void> {
        const succeeded = await this.store.submit();

        if (this.destroyed) return;

        this.visiblePasswords.set(new Set());

        // Changing the password rotates the security stamp, so the current token no longer works.
        if (this.store.requiresSignIn()) this.session.clear();

        this.changeDetector.detectChanges();

        if (!succeeded) {
            this.element.nativeElement
                .querySelector<HTMLElement>('[aria-invalid="true"], [role="alert"]')
                ?.focus();
        }
    }
}
