import {TestBed} from '@angular/core/testing';
import {provideRouter} from '@angular/router';
import {ADMIN_USER_API} from '../application/admin-user-api';
import {RoleAdminPage} from './role-admin.page';
import {RoleAdminStore} from './user-admin.store';
import {ConfirmationService} from '../../../shared/ui/confirmation/confirmation.service';
import {MockConfirmationService} from '../../../../testing/mock-confirmation';

describe('RoleAdminPage', () => {
    const roles = [
        {id: 'admin', roleName: 'Admin', description: 'System administrator', isActive: true},
        {id: 'editor', roleName: 'Editor', description: 'Content editor', isActive: false},
    ];

    async function render(items = roles) {
        const updateRole = vi.fn().mockResolvedValue(undefined);
        await TestBed.configureTestingModule({
            imports: [RoleAdminPage],
            providers: [
                provideRouter([]),
                {provide: ConfirmationService, useClass: MockConfirmationService},
            ],
        })
            .overrideComponent(RoleAdminPage, {
                set: {
                    providers: [
                        RoleAdminStore,
                        {provide: ConfirmationService, useClass: MockConfirmationService},
                        {
                            provide: ADMIN_USER_API,
                        useValue: {
                            listRoles: vi.fn().mockResolvedValue({
                                items,
                                totalCount: items.length,
                                pageNumber: 1,
                                pageSize: 5
                            }),
                            updateRole,
                        },
                    }]
                }
            }).compileComponents();
        const fixture = TestBed.createComponent(RoleAdminPage);
        fixture.detectChanges();
        await fixture.whenStable();
        fixture.detectChanges();
        return {fixture, element: fixture.nativeElement as HTMLElement, updateRole};
    }

    it('exposes the selected role and prevents deleting a system role', async () => {
        const {element} = await render();
        expect(element.querySelector('[aria-pressed="true"]')?.textContent).toContain('Admin');
        expect(element.querySelector<HTMLButtonElement>('.role-delete')?.disabled).toBe(true);
        expect(element.querySelector('.role-note')?.textContent).toContain('cannot be deleted');
        expect(element.querySelector<HTMLInputElement>('[role="switch"]')?.checked).toBe(true);
    });

    it('selects another role and saves the edited form and toggle state', async () => {
        const {fixture, element, updateRole} = await render();
        element.querySelectorAll<HTMLButtonElement>('.role-card')[1].click();
        fixture.detectChanges();
        const name = element.querySelector<HTMLInputElement>('[formControlName="roleName"]')!;
        expect(name.value).toBe('Editor');
        name.value = 'Content editor';
        name.dispatchEvent(new Event('input'));
        element.querySelector<HTMLInputElement>('[role="switch"]')!.click();
        element.querySelector<HTMLFormElement>('form')!.dispatchEvent(new Event('submit', {cancelable: true}));
        await fixture.whenStable();
        expect(updateRole).toHaveBeenCalledWith('editor', {
            roleName: 'Content editor', description: 'Content editor', isActive: true,
        });
    });

    it('shows an empty state instead of an editable blank form', async () => {
        const {element} = await render([]);
        expect(element.textContent).toContain('No roles found');
        expect(element.textContent).toContain('Choose a permission group');
        expect(element.querySelector('form')).toBeNull();
    });
});
