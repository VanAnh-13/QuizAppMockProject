import {inject} from '@angular/core';
import {HttpErrorResponse, HttpInterceptorFn} from '@angular/common/http';
import {catchError, throwError} from 'rxjs';
import {API_CONFIG} from '../config/api.config';
import {AuthSession} from './auth-session';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
    const session = inject(AuthSession);
    const base = new URL(inject(API_CONFIG).baseUrl, document.baseURI);
    const url = new URL(request.url, document.baseURI);
    const prefix = base.pathname.replace(/\/$/, '');
    const isApi =
        url.origin === base.origin &&
        (url.pathname === prefix || url.pathname.startsWith(`${prefix}/`));
    const isPublicAuthEndpoint = isApi &&
        (url.pathname === `${prefix}/auth/login` || url.pathname === `${prefix}/auth/register`);
    const token = isApi && !isPublicAuthEndpoint ? session.token() : null;
    return next(
        token ? request.clone({setHeaders: {Authorization: `Bearer ${token}`}}) : request,
    ).pipe(
        catchError((error: unknown) => {
            if (
                isApi &&
                token &&
                error instanceof HttpErrorResponse &&
                error.status === 401 &&
                session.token() === token
            )
                session.clear();
            return throwError(() => error);
        }),
    );
};
