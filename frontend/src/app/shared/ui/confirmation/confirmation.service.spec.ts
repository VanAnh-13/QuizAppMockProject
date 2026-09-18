import {TestBed} from '@angular/core/testing';
import {ConfirmationService} from './confirmation.service';

describe('ConfirmationService', () => {
    let service: ConfirmationService;

    beforeEach(() => {
        TestBed.configureTestingModule({});
        service = TestBed.inject(ConfirmationService);
    });

    it('sets request signal and resolves true on accept', async () => {
        const confirmPromise = service.confirm({
            title: 'Confirm Title',
            message: 'Confirm Message',
        });

        expect(service.request()).toEqual({
            title: 'Confirm Title',
            message: 'Confirm Message',
            confirmLabel: 'Confirm',
            cancelLabel: 'Cancel',
        });

        service.accept();

        const result = await confirmPromise;
        expect(result).toBe(true);
        expect(service.request()).toBeNull();
    });

    it('sets request signal and resolves false on cancel', async () => {
        const confirmPromise = service.confirm({
            title: 'Delete Item',
            message: 'Are you sure?',
            confirmLabel: 'Xóa',
            cancelLabel: 'Không',
        });

        expect(service.request()?.confirmLabel).toBe('Xóa');
        expect(service.request()?.cancelLabel).toBe('Không');

        service.cancel();

        const result = await confirmPromise;
        expect(result).toBe(false);
        expect(service.request()).toBeNull();
    });

    it('cancels previous pending confirmation when a new one is requested', async () => {
        const firstPromise = service.confirm({
            title: 'First',
            message: 'First message',
        });

        const secondPromise = service.confirm({
            title: 'Second',
            message: 'Second message',
        });

        expect(await firstPromise).toBe(false);

        service.accept();
        expect(await secondPromise).toBe(true);
    });
});
