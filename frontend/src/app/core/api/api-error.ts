import {HttpErrorResponse} from '@angular/common/http';

export function apiErrorMessage(
    error: unknown,
    fallback: string,
    options: { conflictMessage?: string } = {},
): string {
    if (!(error instanceof HttpErrorResponse)) return fallback;

    switch (error.status) {
        case 0:
            return 'Cannot connect to the server. Check your connection and try again.';
        case 400:
            if (typeof error.error?.message === 'string' && error.error.message.trim()) {
                return error.error.message.trim();
            }
            return fallback;
        case 401:
            return 'Please log in again to continue.';
        case 403:
            return 'Your account does not have permission to access this content.';
        case 404:
            return 'This quiz or attempt could not be found.';
        case 409:
            return (
                options.conflictMessage ??
                'This attempt was updated in another session or already submitted. Reload it from the server.'
            );
        case 422:
            return 'Some submitted details are invalid. Please check them and try again.';
        default:
            return fallback;
    }
}
