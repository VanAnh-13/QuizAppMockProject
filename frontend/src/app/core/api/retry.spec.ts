import {HttpErrorResponse} from '@angular/common/http';
import {firstValueFrom, of, throwError} from 'rxjs';
import {retryOnTransientError} from './retry';

describe('retryOnTransientError', () => {
    it('emits values without retrying on success', async () => {
        const source$ = of('success').pipe(retryOnTransientError());
        const result = await firstValueFrom(source$);
        expect(result).toBe('success');
    });

    it('does not retry client errors (4xx)', async () => {
        let attempts = 0;
        const source$ = of(null).pipe(
            () => {
                attempts++;
                return throwError(
                    () => new HttpErrorResponse({status: 404, statusText: 'Not Found'}),
                );
            },
            retryOnTransientError(2),
        );

        await expect(firstValueFrom(source$)).rejects.toMatchObject({status: 404});
        expect(attempts).toBe(1);
    });
});
