import {HttpClient, provideHttpClient, withInterceptors} from '@angular/common/http';
import {HttpTestingController, provideHttpClientTesting} from '@angular/common/http/testing';
import {TestBed} from '@angular/core/testing';
import {firstValueFrom} from 'rxjs';
import {ErrorNotificationService} from './error-notification.service';
import {errorInterceptor} from './error.interceptor';

describe('errorInterceptor', () => {
    let http: HttpClient;
    let httpMock: HttpTestingController;
    let notificationService: ErrorNotificationService;

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [
                provideHttpClient(withInterceptors([errorInterceptor])),
                provideHttpClientTesting(),
            ],
        });

        http = TestBed.inject(HttpClient);
        httpMock = TestBed.inject(HttpTestingController);
        notificationService = TestBed.inject(ErrorNotificationService);
        vi.spyOn(notificationService, 'show');
    });

    afterEach(() => {
        vi.useRealTimers();
        httpMock.verify();
    });

    it('notifies on 500 server error', async () => {
        const promise = firstValueFrom(http.post('/api/test', {}));

        const req = httpMock.expectOne('/api/test');
        req.flush({message: 'Server Error'}, {status: 500, statusText: 'Internal Server Error'});

        await expect(promise).rejects.toBeDefined();
        expect(notificationService.show).toHaveBeenCalledWith(
            'A server error occurred. Please try again later.',
        );
    });

    it('notifies on status 0 network error', async () => {
        const promise = firstValueFrom(http.post('/api/test', {}));

        const req = httpMock.expectOne('/api/test');
        req.error(new ProgressEvent('error'), {status: 0, statusText: 'Unknown Error'});

        await expect(promise).rejects.toBeDefined();
        expect(notificationService.show).toHaveBeenCalledWith(
            'Cannot connect to the server. Check your connection and try again.',
        );
    });

    it('does not notify when a transient GET error succeeds on retry', async () => {
        vi.useFakeTimers();
        const promise = firstValueFrom(http.get('/api/test'));

        httpMock
            .expectOne('/api/test')
            .flush({message: 'Server Error'}, {status: 500, statusText: 'Internal Server Error'});
        await vi.advanceTimersByTimeAsync(1000);
        httpMock.expectOne('/api/test').flush({result: 'success'});

        await expect(promise).resolves.toEqual({result: 'success'});
        expect(notificationService.show).not.toHaveBeenCalled();
    });

    it('notifies once after transient GET retries are exhausted', async () => {
        vi.useFakeTimers();
        const promise = firstValueFrom(http.get('/api/test'));
        const serverError = {status: 500, statusText: 'Internal Server Error'};

        httpMock.expectOne('/api/test').flush({message: 'Server Error'}, serverError);
        await vi.advanceTimersByTimeAsync(1000);
        httpMock.expectOne('/api/test').flush({message: 'Server Error'}, serverError);
        await vi.advanceTimersByTimeAsync(2000);
        httpMock.expectOne('/api/test').flush({message: 'Server Error'}, serverError);

        await expect(promise).rejects.toMatchObject({status: 500});
        expect(notificationService.show).toHaveBeenCalledTimes(1);
        expect(notificationService.show).toHaveBeenCalledWith(
            'A server error occurred. Please try again later.',
        );
    });

    it('does not notify on 4xx client errors', async () => {
        const promise = firstValueFrom(http.get('/api/test'));

        const req = httpMock.expectOne('/api/test');
        req.flush({message: 'Not found'}, {status: 404, statusText: 'Not Found'});

        await expect(promise).rejects.toBeDefined();
        expect(notificationService.show).not.toHaveBeenCalled();
    });
});
