import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {ReactiveFormsModule} from '@angular/forms';
import {AdminShellComponent, initialsFrom} from '../../admin-question-bank/presentation/admin-shell.component';
import {provideAdminUsers} from '../infrastructure/admin-user.provider';
import {UserAdminStore} from './user-admin.store';

@Component({
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [AdminShellComponent, ReactiveFormsModule],
    providers: [provideAdminUsers(), UserAdminStore],
    selector: 'app-user-admin-page',
    styleUrl: './user-admin.page.css',
    templateUrl: './user-admin.page.html',
})
export class UserAdminPage {
    protected readonly store = inject(UserAdminStore);
    protected readonly initialsFrom = initialsFrom;
}
