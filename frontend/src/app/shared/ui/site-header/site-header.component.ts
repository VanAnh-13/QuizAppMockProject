import {
    ChangeDetectionStrategy,
    Component,
    ElementRef,
    HostListener,
    inject,
    input,
    output,
    signal,
    viewChild,
} from '@angular/core';
import {Router, RouterLink, RouterLinkActive} from '@angular/router';
import {BrandComponent} from '../brand/brand.component';
import {AuthSession} from '../../../core/auth/auth-session';
import {ConfirmationService} from '../confirmation/confirmation.service';

@Component({
    selector: 'app-site-header',
    imports: [BrandComponent, RouterLink, RouterLinkActive],
    styleUrl: './site-header.component.css',
    changeDetection: ChangeDetectionStrategy.OnPush,
    templateUrl: './site-header.component.html',
})
export class SiteHeaderComponent {
    readonly sessionChanged = output<void>();
    readonly returnAfterAuth = input<string | null>(null);
    readonly isMenuOpen = signal(false);
    protected readonly session = inject(AuthSession);
    protected readonly router = inject(Router);
    private readonly confirmation = inject(ConfirmationService);
    private readonly userMenu = viewChild<ElementRef<HTMLDetailsElement>>('userMenu');

    protected onMenuToggle(event: Event): void {
        const details = event.target as HTMLDetailsElement;
        this.isMenuOpen.set(details.open);
    }

    protected closeMenu(): void {
        const menu = this.userMenu()?.nativeElement;
        if (menu) {
            menu.open = false;
        }
        this.isMenuOpen.set(false);
    }

    protected async logout(): Promise<void> {
        this.closeMenu();
        const confirmed = await this.confirmation.confirm({
            title: 'Are you sure to log out?',
            message: 'Are you sure you want to log out of your account?',
            confirmLabel: 'Yes',
            cancelLabel: 'No',
            variant: 'primary',
            icon: 'logout',
        });
        if (!confirmed) return;

        this.session.clear();
        this.sessionChanged.emit();
        void this.router.navigateByUrl('/login');
    }

    @HostListener('document:click', ['$event'])
    protected onDocumentClick(event: MouseEvent): void {
        const menu = this.userMenu()?.nativeElement;
        if (menu?.open && !menu.contains(event.target as Node)) {
            menu.open = false;
            this.isMenuOpen.set(false);
        }
    }
}
