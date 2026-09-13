import { TestBed } from '@angular/core/testing';
import { ApiClient } from '../../../core/api/api-client';
import { AuthSession } from '../../../core/auth/auth-session';
import { DialogComponent } from '../dialog/dialog.component';
import { AuthDialogComponent } from './auth-dialog.component';

describe('AuthDialogComponent', () => {
  it('sends the complete registration contract', async () => {
    const post = vi.fn().mockResolvedValue({});
    vi.spyOn(DialogComponent.prototype, 'open').mockImplementation(() => {});
    TestBed.configureTestingModule({
      imports: [AuthDialogComponent],
      providers: [
        { provide: ApiClient, useValue: { post } },
        { provide: AuthSession, useValue: { set: vi.fn() } },
      ],
    });
    const fixture = TestBed.createComponent(AuthDialogComponent);
    fixture.detectChanges();
    fixture.componentInstance.open(true);
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;
    setInput(element, 'username', 'learner');
    setInput(element, 'fullName', 'Quiz Learner');
    setInput(element, 'email', 'learner@example.com');
    setInput(element, 'password', 'Password-123!');
    setInput(element, 'confirmPassword', 'Password-123!');

    element
      .querySelector('form')!
      .dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    await fixture.whenStable();

    expect(post).toHaveBeenCalledWith('auth/register', {
      username: 'learner',
      email: 'learner@example.com',
      password: 'Password-123!',
      confirmPassword: 'Password-123!',
      profile: { fullName: 'Quiz Learner' },
    });
  });
});

function setInput(element: HTMLElement, controlName: string, value: string): void {
  const input = element.querySelector<HTMLInputElement>(`[formcontrolname="${controlName}"]`)!;
  input.value = value;
  input.dispatchEvent(new Event('input'));
}
