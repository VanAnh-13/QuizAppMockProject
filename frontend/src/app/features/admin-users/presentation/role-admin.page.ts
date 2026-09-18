import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {ReactiveFormsModule} from '@angular/forms';
import {AdminShellComponent} from '../../admin-question-bank/presentation/admin-shell.component';
import {provideAdminUsers} from '../infrastructure/admin-user.provider';
import {RoleAdminStore} from './user-admin.store';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [AdminShellComponent, ReactiveFormsModule],
    providers: [provideAdminUsers(), RoleAdminStore],
    selector: 'app-role-admin-page',
    styleUrl: './role-admin.page.css',
    templateUrl: './role-admin.page.html',
})
export class RoleAdminPage {
    protected readonly store = inject(RoleAdminStore);
}
