import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { ApiClient } from '../../../core/api/api-client';
import { AuthResponse, AuthSession } from '../../../core/auth/auth-session';
import { AuthDialogComponent } from '../auth-dialog/auth-dialog.component';
import { SiteHeaderComponent } from './site-header.component';

describe('SiteHeaderComponent', () => {
  it('renders the mobile menu for small screens', () => {
    configure(null);
    const fixture = TestBed.createComponent(SiteHeaderComponent);
    fixture.detectChanges();

    const menu = (fixture.nativeElement as HTMLElement).querySelector(
      'details.header__mobile-menu',
    );

    expect(menu).toBeTruthy();
    expect(menu?.querySelector('summary')).toBeTruthy();
  });

  it('opens the registration dialog from the mobile menu', () => {
    const open = vi.spyOn(AuthDialogComponent.prototype, 'open').mockImplementation(() => {});
    configure(null);
    const fixture = TestBed.createComponent(SiteHeaderComponent);
    fixture.detectChanges();

    mobileAction(fixture, 'Đăng ký').click();

    expect(open).toHaveBeenCalledWith(true);
  });

  it('opens the login dialog from the mobile menu', () => {
    const open = vi.spyOn(AuthDialogComponent.prototype, 'open').mockImplementation(() => {});
    configure(null);
    const fixture = TestBed.createComponent(SiteHeaderComponent);
    fixture.detectChanges();

    mobileAction(fixture, 'Đăng nhập').click();

    expect(open).toHaveBeenCalledWith();
  });

  it('clears the session and emits sessionChanged on mobile logout', () => {
    const { clear } = configure({ id: '1', username: 'learner', fullName: 'Quiz Learner' });
    const fixture = TestBed.createComponent(SiteHeaderComponent);
    fixture.detectChanges();
    const emitted = vi.fn();
    fixture.componentInstance.sessionChanged.subscribe(emitted);

    mobileAction(fixture, 'Đăng xuất').click();

    expect(clear).toHaveBeenCalledTimes(1);
    expect(emitted).toHaveBeenCalledTimes(1);
  });
});

function configure(user: AuthResponse['userDto'] | null) {
  const clear = vi.fn();
  TestBed.configureTestingModule({
    imports: [SiteHeaderComponent],
    providers: [
      { provide: ApiClient, useValue: { post: vi.fn() } },
      { provide: AuthSession, useValue: { user: signal(user), clear } },
      provideRouter([]),
    ],
  });
  return { clear };
}

function mobileAction(
  fixture: ComponentFixture<SiteHeaderComponent>,
  label: string,
): HTMLButtonElement {
  const actions = (fixture.nativeElement as HTMLElement).querySelector('.header__mobile-actions');
  const button = Array.from(actions?.querySelectorAll('button') ?? []).find(
    (candidate) => candidate.textContent?.trim() === label,
  );
  expect(button, `mobile action "${label}"`).toBeTruthy();
  return button as HTMLButtonElement;
}
