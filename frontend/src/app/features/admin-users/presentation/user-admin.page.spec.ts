import {TestBed} from '@angular/core/testing';
import {provideRouter} from '@angular/router';
import {ADMIN_USER_API} from '../application/admin-user-api';
import {UserAdminPage} from './user-admin.page';
import {UserAdminStore} from './user-admin.store';
import {ConfirmationService} from '../../../shared/ui/confirmation/confirmation.service';
import {MockConfirmationService} from '../../../../testing/mock-confirmation';

describe('UserAdminPage', () => {
    const roles = [
        {id: 'r-admin', roleName: 'Admin', description: 'System administrator', isActive: true},
        {id: 'r-user', roleName: 'User', description: 'Quiz participant', isActive: true},
    ];

    const users = [
        {
            id: 'u-1',
            username: 'alice',
            email: 'alice@example.com',
            fullName: 'Alice Smith',
            isActive: true,
            roles: [{id: 'r-admin', roleName: 'Admin', description: 'System administrator', isActive: true}],
        },
        {
            id: 'u-2',
            username: 'bob',
            email: 'bob@example.com',
            fullName: 'Bob Jones',
            isActive: true,
            roles: [],
        },
    ];

    async function render(userItems = users, roleItems = roles) {
        const updateUserRoles = vi.fn().mockResolvedValue(undefined);
        const setUserActive = vi.fn().mockResolvedValue(undefined);
        await TestBed.configureTestingModule({
            imports: [UserAdminPage],
            providers: [
                provideRouter([]),
                {provide: ConfirmationService, useClass: MockConfirmationService},
            ],
        })
            .overrideComponent(UserAdminPage, {
                set: {
                    providers: [
                        UserAdminStore,
                        {provide: ConfirmationService, useClass: MockConfirmationService},
                        {
                            provide: ADMIN_USER_API,
                            useValue: {
                                listUsers: vi.fn().mockResolvedValue({
                                    items: userItems,
                                    totalCount: userItems.length,
                                    pageNumber: 1,
                                    pageSize: 5,
                                }),
                                listRoles: vi.fn().mockResolvedValue({
                                    items: roleItems,
                                    totalCount: roleItems.length,
                                    pageNumber: 1,
                                    pageSize: 100,
                                }),
                                updateUserRoles,
                                setUserActive,
                            },
                        },
                    ],
                },
            })
            .compileComponents();
        const fixture = TestBed.createComponent(UserAdminPage);
        fixture.detectChanges();
        await fixture.whenStable();
        fixture.detectChanges();
        return {fixture, element: fixture.nativeElement as HTMLElement, updateUserRoles, setUserActive};
    }

    it('displays user cards with role tags and selects the first user', async () => {
        const {element} = await render();
        expect(element.querySelector('[aria-pressed="true"]')?.textContent).toContain('Alice Smith');
        expect(element.querySelector('.user-tag')?.textContent).toContain('Admin');
        expect(element.querySelector('#user-editor-heading')?.textContent).toContain('Alice Smith');
        const checkboxes = element.querySelectorAll<HTMLInputElement>('.user-role-checkbox');
        expect(checkboxes.length).toBe(2);
        expect(checkboxes[0].checked).toBe(true);
        expect(checkboxes[1].checked).toBe(false);
    });

    it('assigns a role to a user and calls updateUserRoles on save', async () => {
        const {fixture, element, updateUserRoles} = await render();
        // Select second user (bob)
        element.querySelectorAll<HTMLButtonElement>('.user-card')[1].click();
        fixture.detectChanges();
        expect(element.querySelector('#user-editor-heading')?.textContent).toContain('Bob Jones');

        const checkboxes = element.querySelectorAll<HTMLInputElement>('.user-role-checkbox');
        expect(checkboxes[0].checked).toBe(false);
        expect(checkboxes[1].checked).toBe(false);

        // Check the second role (User)
        checkboxes[1].click();
        fixture.detectChanges();
        expect(checkboxes[1].checked).toBe(true);

        // Click Save roles
        element.querySelector<HTMLButtonElement>('.primary-action')!.click();
        await fixture.whenStable();

        expect(updateUserRoles).toHaveBeenCalledWith('u-2', ['r-user']);
    });
});
