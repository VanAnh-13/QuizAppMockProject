import {HttpErrorResponse} from '@angular/common/http';
import {apiErrorMessage} from './api-error';

describe('apiErrorMessage', () => {
    it('preserves quiz conflict guidance when no override is supplied', () => {
        expect(apiErrorMessage(new HttpErrorResponse({status: 409}), 'Fallback')).toBe(
            'This attempt was updated in another session or already submitted. Reload it from the server.',
        );
    });

    it('keeps non-conflict errors independent of the conflict override', () => {
        expect(
            apiErrorMessage(new HttpErrorResponse({status: 0}), 'Fallback', {
                conflictMessage: 'Account already exists.',
            }),
        ).toBe('Cannot connect to the server. Check your connection and try again.');
    });

    it('returns the backend error message for 400 bad request when present', () => {
        expect(
            apiErrorMessage(
                new HttpErrorResponse({status: 400, error: {message: 'You cannot remove your own administrator role.'}}),
                'Fallback',
            ),
        ).toBe('You cannot remove your own administrator role.');
    });

    it('uses the fallback for unknown failures even with a conflict override', () => {
        expect(
            apiErrorMessage(new Error('Unexpected error'), 'Fallback', {
                conflictMessage: 'Account already exists.',
            }),
        ).toBe('Fallback');
    });
});
