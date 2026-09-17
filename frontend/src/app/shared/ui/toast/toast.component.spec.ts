import {TestBed} from '@angular/core/testing';
import {ErrorNotificationService} from '../../../core/errors/error-notification.service';
import {ToastComponent} from './toast.component';

describe('ToastComponent', () => {
    it('announces each toast once without a nested live region', () => {
        TestBed.configureTestingModule({
            imports: [ToastComponent],
        });

        const fixture = TestBed.createComponent(ToastComponent);
        TestBed.inject(ErrorNotificationService).show('Không thể tải danh sách quiz.');
        fixture.detectChanges();
        const element = fixture.nativeElement as HTMLElement;
        const container = element.querySelector('.toast-container');
        const toast = element.querySelector('.toast');

        expect(container?.hasAttribute('aria-live')).toBe(false);
        expect(toast?.getAttribute('role')).toBe('alert');
        expect(element.querySelectorAll('[aria-live], [role="alert"]')).toHaveLength(1);
    });
});
