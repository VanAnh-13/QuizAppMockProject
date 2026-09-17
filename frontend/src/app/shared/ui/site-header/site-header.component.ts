import {
    ChangeDetectionStrategy,
    Component,
    ElementRef,
    HostListener,
    inject,
    input,
    output,
    viewChild,
} from '@angular/core';
import {Router, RouterLink} from '@angular/router';
import {BrandComponent} from '../brand/brand.component';
import {AuthSession} from '../../../core/auth/auth-session';
import {SiteInfoDialogComponent} from '../site-info-dialog/site-info-dialog.component';

@Component({
    selector: 'app-site-header',
    imports: [BrandComponent, RouterLink, SiteInfoDialogComponent],
    styleUrl: './site-header.component.css',
    changeDetection: ChangeDetectionStrategy.OnPush,
    templateUrl: './site-header.component.html',
})
export class SiteHeaderComponent {
    protected readonly session = inject(AuthSession);
    protected readonly router = inject(Router);
    private readonly userMenu = viewChild<ElementRef<HTMLDetailsElement>>('userMenu');
    readonly sessionChanged = output<void>();
    readonly returnAfterAuth = input<string | null>(null);

    protected logout(): void {
        this.session.clear();
        this.sessionChanged.emit();
    }

    @HostListener('document:click', ['$event'])
    protected onDocumentClick(event: MouseEvent): void {
        const menu = this.userMenu()?.nativeElement;
        if (menu?.open && !menu.contains(event.target as Node)) {
            menu.open = false;
        }
    }
}
