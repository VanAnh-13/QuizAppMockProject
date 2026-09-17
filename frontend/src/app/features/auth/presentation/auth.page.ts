import {
    afterNextRender,
    ChangeDetectionStrategy,
    ChangeDetectorRef,
    Component,
    DestroyRef,
    ElementRef,
    inject,
    signal
} from '@angular/core';
import {ViewportScroller} from '@angular/common';
import {ReactiveFormsModule} from '@angular/forms';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {SiteHeaderComponent} from '../../../shared/ui/site-header/site-header.component';
import {SiteInfoDialogComponent} from '../../../shared/ui/site-info-dialog/site-info-dialog.component';
import {authReturnUrl} from '../domain/auth-contracts';
import {AuthFormStore} from './auth-form.store';
import {LOGIN_FIELDS, REGISTER_FIELDS} from './auth-page.config';

@Component({
    selector: 'app-auth-page',
    imports: [ReactiveFormsModule, RouterLink, SiteHeaderComponent, SiteInfoDialogComponent],
    providers: [AuthFormStore],
    changeDetection: ChangeDetectionStrategy.OnPush,
    templateUrl: './auth.page.html',
    styleUrl: './auth.page.css',
})
export class AuthPage {
    protected readonly store = inject(AuthFormStore);
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly element = inject<ElementRef<HTMLElement>>(ElementRef);
    private readonly changeDetector = inject(ChangeDetectorRef);
    private readonly viewport = inject(ViewportScroller);
    protected readonly registering = this.route.snapshot.data['registering'] === true;
    protected readonly returnUrl = authReturnUrl(this.route.snapshot.queryParamMap.get('returnUrl'));
    protected readonly registered = !this.registering && this.router.currentNavigation()?.extras.state?.['registered'] === true;
    protected readonly fields = this.registering ? REGISTER_FIELDS : LOGIN_FIELDS;
    protected readonly year = new Date().getFullYear();
    protected readonly visiblePasswords = signal<ReadonlySet<string>>(new Set());
    private destroyed = false;

    constructor() {
        this.store.configure(this.registering);
        inject(DestroyRef).onDestroy(() => {
            this.destroyed = true;
        });
        afterNextRender(() => {
            this.viewport.scrollToPosition([0, 0]);
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
        if (this.destroyed) {
            return;
        }
        this.visiblePasswords.set(new Set());
        if (!succeeded) {
            this.changeDetector.detectChanges();
            this.element.nativeElement.querySelector<HTMLElement>('[aria-invalid="true"], [role="alert"]')?.focus();
            return;
        }
        if (this.registering) {
            await this.router.navigate(['/login'], {
                queryParams: {returnUrl: this.returnUrl},
                state: {registered: true},
            });
        } else {
            await this.router.navigateByUrl(this.returnUrl);
        }
    }
}
