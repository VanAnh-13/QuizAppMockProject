import {inject} from '@angular/core';
import {HttpErrorResponse, HttpInterceptorFn} from '@angular/common/http';
import {catchError, throwError} from 'rxjs';
import {retryOnTransientError} from '../api/retry';
import {ErrorNotificationService} from './error-notification.service';

export const errorInterceptor: HttpInterceptorFn = (request, next) => {
    const notifications = inject(ErrorNotificationService);

    const response$ = next(request);
    const retriedResponse$ = request.method === 'GET'
        ? response$.pipe(retryOnTransientError())
        : response$;

    return retriedResponse$.pipe(
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
        return 'Cannot connect to the server. Check your connection and try again.';
    }
    if (status === 503) {
        return 'The server is undergoing maintenance. Please try again later.';
    }
    return 'A server error occurred. Please try again later.';
}
