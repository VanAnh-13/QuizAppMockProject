import {HttpErrorResponse} from '@angular/common/http';
import {MonoTypeOperatorFunction, retry, throwError, timer} from 'rxjs';

export function retryOnTransientError<T>(maxRetries = 2): MonoTypeOperatorFunction<T> {
    return retry<T>({
        count: maxRetries,
        delay: (error: unknown, retryCount: number) => {
            if (error instanceof HttpErrorResponse && error.status > 0 && error.status < 500) {
                return throwError(() => error);
            }
            return timer(retryCount * 1000);
        },
    });
}
