import {TestBed} from '@angular/core/testing';
import {ErrorNotificationService} from './error-notification.service';

describe('ErrorNotificationService', () => {
    let service: ErrorNotificationService;

    beforeEach(() => {
        vi.useFakeTimers();
        TestBed.configureTestingModule({});
        service = TestBed.inject(ErrorNotificationService);
    });

    afterEach(() => {
        vi.useRealTimers();
    });

    it('adds notification with unique id and auto-dismisses after duration', () => {
        service.show('Test error', {duration: 3000});

        expect(service.notifications().length).toBe(1);
        expect(service.notifications()[0].message).toBe('Test error');

        vi.advanceTimersByTime(2999);
        expect(service.notifications().length).toBe(1);

        vi.advanceTimersByTime(1);
        expect(service.notifications().length).toBe(0);
    });

    it('dismisses notification explicitly by id', () => {
        service.show('Error 1', {duration: 10000});
        service.show('Error 2', {duration: 10000});

        expect(service.notifications().length).toBe(2);
        const idToDismiss = service.notifications()[0].id;

        service.dismiss(idToDismiss);
        expect(service.notifications().length).toBe(1);
        expect(service.notifications()[0].message).toBe('Error 2');
    });
});
