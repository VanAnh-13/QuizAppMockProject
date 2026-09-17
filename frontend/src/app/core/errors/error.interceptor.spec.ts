import {HttpClient, provideHttpClient, withInterceptors} from '@angular/common/http';
import {HttpTestingController, provideHttpClientTesting} from '@angular/common/http/testing';
import {TestBed} from '@angular/core/testing';
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
        httpMock.verify();
    });

    it('notifies on 500 server error', async () => {
        const promise = http.get('/api/test').toPromise();

        const req = httpMock.expectOne('/api/test');
        req.flush({message: 'Server Error'}, {status: 500, statusText: 'Internal Server Error'});

        await expect(promise).rejects.toBeDefined();
        expect(notificationService.show).toHaveBeenCalledWith(
            'Đã xảy ra lỗi phía máy chủ. Vui lòng thử lại sau.',
        );
    });

    it('notifies on status 0 network error', async () => {
        const promise = http.get('/api/test').toPromise();

        const req = httpMock.expectOne('/api/test');
        req.error(new ProgressEvent('error'), {status: 0, statusText: 'Unknown Error'});

        await expect(promise).rejects.toBeDefined();
        expect(notificationService.show).toHaveBeenCalledWith(
            'Không thể kết nối máy chủ. Kiểm tra kết nối mạng rồi thử lại.',
        );
    });

    it('does not notify on 4xx client errors', async () => {
        const promise = http.get('/api/test').toPromise();

        const req = httpMock.expectOne('/api/test');
        req.flush({message: 'Not found'}, {status: 404, statusText: 'Not Found'});

        await expect(promise).rejects.toBeDefined();
        expect(notificationService.show).not.toHaveBeenCalled();
    });
});
