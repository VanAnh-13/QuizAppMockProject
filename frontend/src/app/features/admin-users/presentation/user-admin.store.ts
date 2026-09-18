import {computed, inject, Injectable, signal} from '@angular/core';
import {FormControl, FormGroup, Validators} from '@angular/forms';
import {debounceTime} from 'rxjs';
import {apiErrorMessage} from '../../../core/api/api-error';
import {ADMIN_USER_API, ADMIN_USER_CONFIG} from '../application/admin-user-api';
import {AdminRole, AdminUser} from '../domain/admin-user';
import {initialsFrom} from '../../admin-question-bank/presentation/admin-shell.component';
import {ConfirmationService} from '../../../shared/ui/confirmation/confirmation.service';

@Injectable()
export class UserAdminStore {
    readonly searchControl = new FormControl('', {nonNullable: true});
    readonly pageSize = inject(ADMIN_USER_CONFIG).pageSize;
    readonly users = signal<readonly AdminUser[]>([]);
    readonly roles = signal<readonly AdminRole[]>([]);
    readonly selectedId = signal<string | null>(null);
    readonly selectedRoleIds = signal<readonly string[]>([]);
    readonly totalCount = signal(0);
    readonly currentPage = signal(1);
    readonly isLoading = signal(true);
    readonly isSaving = signal(false);
    readonly errorMessage = signal<string | null>(null);
    readonly selected = computed(() => this.users().find((user) => user.id === this.selectedId()) ?? null);
    readonly rangeStart = computed(() => (this.totalCount() === 0 ? 0 : (this.currentPage() - 1) * this.pageSize + 1));
    readonly rangeEnd = computed(() =>
        Math.min((this.currentPage() - 1) * this.pageSize + this.users().length, this.totalCount()),
    );
    readonly hasPreviousPage = computed(() => this.currentPage() > 1);
    readonly hasNextPage = computed(() => this.currentPage() * this.pageSize < this.totalCount());
    readonly initials = computed(() => initialsFrom(this.selected()?.fullName, this.selected()?.username));
    private readonly api = inject(ADMIN_USER_API);
    private readonly confirmation = inject(ConfirmationService);

    constructor() {
        this.searchControl.valueChanges.pipe(debounceTime(250)).subscribe(() => void this.load(1));
        void this.refreshRoles();
        void this.load(1);
    }

    async load(pageNumber = this.currentPage()): Promise<void> {
        this.isLoading.set(true);
        this.errorMessage.set(null);
        try {
            const page = await this.api.listUsers(pageNumber, this.pageSize, this.searchControl.value.trim());
            this.users.set(page.items);
            this.totalCount.set(page.totalCount);
            this.currentPage.set(pageNumber);
            const currentSelected = page.items.find((item) => item.id === this.selectedId());
            if (currentSelected) {
                this.selectedRoleIds.set(currentSelected.roles.map((role) => role.id));
            } else if (page.items[0]) {
                this.select(page.items[0]);
            } else {
                this.selectedId.set(null);
                this.selectedRoleIds.set([]);
            }
        } catch (error) {
            this.users.set([]);
            this.errorMessage.set(apiErrorMessage(error, 'Could not load users.'));
        } finally {
            this.isLoading.set(false);
        }
    }

    select(user: AdminUser): void {
        this.selectedId.set(user.id);
        this.selectedRoleIds.set(user.roles.map((role) => role.id));
    }

    toggleRole(roleId: string): void {
        const current = this.selectedRoleIds();
        this.selectedRoleIds.set(
            current.includes(roleId) ? current.filter((id) => id !== roleId) : [...current, roleId],
        );
    }

    async saveRoles(): Promise<void> {
        const user = this.selected();
        if (!user) return;
        const confirmed = await this.confirmation.confirm({
            title: 'Are you sure to save roles for this user?',
            message: `Update role assignments for "${user.fullName || user.username}"?`,
            confirmLabel: 'Yes',
            cancelLabel: 'No',
            variant: 'primary',
            icon: 'help_outline',
        });
        if (!confirmed) return;

        this.isSaving.set(true);
        this.errorMessage.set(null);
        try {
            await this.api.updateUserRoles(user.id, this.selectedRoleIds());
            await this.load(this.currentPage());
            void this.confirmation.notify({
                title: 'Roles updated',
                message: `Roles for "${user.fullName || user.username}" have been saved successfully.`,
                variant: 'success',
                icon: 'verified_user',
            });
        } catch (error) {
            this.errorMessage.set(apiErrorMessage(error, 'Could not save roles.'));
        } finally {
            this.isSaving.set(false);
        }
    }

    async toggleActive(): Promise<void> {
        const user = this.selected();
        if (!user) return;
        const willLock = user.isActive;
        const confirmed = await this.confirmation.confirm({
            title: willLock ? 'Are you sure to lock this user account?' : 'Are you sure to unlock this user account?',
            message: `Are you sure you want to ${willLock ? 'lock' : 'unlock'} the account for "${user.fullName || user.username}"?`,
            confirmLabel: 'Yes',
            cancelLabel: 'No',
            variant: willLock ? 'danger' : 'primary',
            icon: willLock ? 'lock' : 'lock_open',
        });
        if (!confirmed) return;

        try {
            await this.api.setUserActive(user.id, !user.isActive);
            await this.load(this.currentPage());
            void this.confirmation.notify({
                title: willLock ? 'Account locked' : 'Account unlocked',
                message: `The account for "${user.fullName || user.username}" has been ${willLock ? 'locked' : 'unlocked'}.`,
                variant: 'success',
                icon: willLock ? 'lock' : 'lock_open',
            });
        } catch (error) {
            this.errorMessage.set(apiErrorMessage(error, `Could not ${willLock ? 'lock' : 'unlock'} account.`));
        }
    }

    private async refreshRoles(): Promise<void> {
        const page = await this.api.listRoles(1, 100, '');
        this.roles.set(page.items);
    }
}

@Injectable()
export class RoleAdminStore {
    readonly form = new FormGroup({
        roleName: new FormControl('', {nonNullable: true, validators: [Validators.required]}),
        description: new FormControl('', {nonNullable: true}),
        isActive: new FormControl(true, {nonNullable: true}),
    });
    readonly pageSize = inject(ADMIN_USER_CONFIG).pageSize;
    readonly roles = signal<readonly AdminRole[]>([]);
    readonly totalCount = signal(0);
    readonly currentPage = signal(1);
    readonly selectedId = signal<string | null>(null);
    readonly isLoading = signal(true);
    readonly isSaving = signal(false);
    readonly errorMessage = signal<string | null>(null);
    readonly selected = computed(() => this.roles().find((role) => role.id === this.selectedId()) ?? null);
    readonly protectedRole = computed(() => {
        const name = this.selected()?.roleName.toLowerCase() ?? '';
        return name === 'admin' || name === 'student' || name === 'học viên' || name === 'quản trị viên';
    });
    private readonly api = inject(ADMIN_USER_API);
    private readonly confirmation = inject(ConfirmationService);

    constructor() {
        void this.load(1);
    }

    async load(pageNumber = this.currentPage()): Promise<void> {
        this.isLoading.set(true);
        this.errorMessage.set(null);
        try {
            const page = await this.api.listRoles(pageNumber, this.pageSize, '');
            this.roles.set(page.items);
            this.totalCount.set(page.totalCount);
            this.currentPage.set(pageNumber);
            if (!this.selected() && page.items[0]) this.select(page.items[0]);
        } catch (error) {
            this.roles.set([]);
            this.errorMessage.set(apiErrorMessage(error, 'Could not load roles.'));
        } finally {
            this.isLoading.set(false);
        }
    }

    select(role: AdminRole): void {
        this.selectedId.set(role.id);
        this.form.reset({
            roleName: role.roleName,
            description: role.description ?? '',
            isActive: role.isActive,
        });
    }

    async save(): Promise<void> {
        this.form.markAllAsTouched();
        if (this.form.invalid || !this.selected()) return;
        const value = this.form.getRawValue();

        const confirmed = await this.confirmation.confirm({
            title: 'Are you sure to save changes to this role?',
            message: `Save updates for role "${value.roleName.trim()}"?`,
            confirmLabel: 'Yes',
            cancelLabel: 'No',
            variant: 'primary',
            icon: 'help_outline',
        });
        if (!confirmed) return;

        this.isSaving.set(true);
        this.errorMessage.set(null);
        try {
            await this.api.updateRole(this.selected()!.id, {
                roleName: value.roleName.trim(),
                description: value.description.trim() || null,
                isActive: value.isActive,
            });
            await this.load(this.currentPage());
            void this.confirmation.notify({
                title: 'Role saved',
                message: `Changes to "${value.roleName.trim()}" have been saved successfully.`,
                variant: 'success',
                icon: 'check_circle',
            });
        } catch (error) {
            this.errorMessage.set(apiErrorMessage(error, 'Could not save the role.'));
        } finally {
            this.isSaving.set(false);
        }
    }

    async remove(): Promise<void> {
        const role = this.selected();
        if (!role || this.protectedRole()) return;
        const confirmed = await this.confirmation.confirm({
            title: 'Are you sure to delete this role?',
            message: `Are you sure you want to delete "${role.roleName}"? This action cannot be undone.`,
            confirmLabel: 'Yes',
            cancelLabel: 'No',
            variant: 'danger',
            icon: 'delete',
        });
        if (!confirmed) return;

        try {
            await this.api.deleteRole(role.id);
            this.selectedId.set(null);
            await this.load(1);
            void this.confirmation.notify({
                title: 'Role deleted',
                message: `The role "${role.roleName}" has been deleted.`,
                variant: 'success',
                icon: 'delete',
            });
        } catch (error) {
            this.errorMessage.set(apiErrorMessage(error, 'Could not delete the role.'));
        }
    }
}
