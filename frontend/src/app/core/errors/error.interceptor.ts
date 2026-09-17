import {inject} from '@angular/core';
import {HttpErrorResponse, HttpInterceptorFn} from '@angular/common/http';
import {catchError, throwError} from 'rxjs';
import {ErrorNotificationService} from './error-notification.service';

export const errorInterceptor: HttpInterceptorFn = (request, next) => {
    const notifications = inject(ErrorNotificationService);

    return next(request).pipe(
        catchError((error: unknown) => {
            if (error instanceof HttpErrorResponse && shouldNotify(error.status)) {
                notifications.show(messageForStatus(error.status));
            }
            return throwError(() => error);
        }),
    );
};

function shouldNotify(status: number): boolean {
    return status === 0 || status >= 500;
}

function messageForStatus(status: number): string {
    if (status === 0) {
        return 'Không thể kết nối máy chủ. Kiểm tra kết nối mạng rồi thử lại.';
    }
    if (status === 503) {
        return 'Máy chủ đang bảo trì. Vui lòng thử lại sau.';
    }
    return 'Đã xảy ra lỗi phía máy chủ. Vui lòng thử lại sau.';
}
