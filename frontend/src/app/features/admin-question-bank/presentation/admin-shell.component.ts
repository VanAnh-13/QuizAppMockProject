import {ChangeDetectionStrategy, Component, computed, inject, input, ViewEncapsulation} from '@angular/core';
import {Router, RouterLink} from '@angular/router';
import {AuthSession} from '../../../core/auth/auth-session';
import {ConfirmationService} from '../../../shared/ui/confirmation/confirmation.service';

export type AdminNavItem = 'quizzes' | 'questions' | 'users' | 'roles';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    encapsulation: ViewEncapsulation.None,
    imports: [RouterLink],
    selector: 'app-admin-shell',
    styleUrl: './admin-shell.component.css',
    templateUrl: './admin-shell.component.html',
})
export class AdminShellComponent {
    readonly active = input<AdminNavItem>('questions');
    protected readonly initials = computed(() => initialsFrom(this.user()?.fullName, this.user()?.username));
    private readonly session = inject(AuthSession);
    protected readonly user = computed(() => this.session.user());
    private readonly router = inject(Router);
    private readonly confirmation = inject(ConfirmationService);

    protected async logout(): Promise<void> {
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
        void this.router.navigateByUrl('/login');
    }
}

export function initialsFrom(fullName: string | null | undefined, username: string | null | undefined): string {
    const source = fullName?.trim() || username?.trim() || 'QT';
    const parts = source.split(/\s+/).filter(Boolean);
    if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
    return `${parts[0][0]}${parts[parts.length - 1][0]}`.toUpperCase();
}
