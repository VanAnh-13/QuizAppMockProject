import {HttpErrorResponse} from '@angular/common/http';
import {apiErrorMessage} from './api-error';

describe('apiErrorMessage', () => {
    it('preserves quiz conflict guidance when no override is supplied', () => {
        expect(apiErrorMessage(new HttpErrorResponse({status: 409}), 'Fallback')).toBe(
            'Dữ liệu đã thay đổi ở phiên khác hoặc bài đã được nộp. Hãy tải lại trạng thái từ máy chủ.',
        );
    });

    it('keeps non-conflict errors independent of the conflict override', () => {
        expect(
            apiErrorMessage(new HttpErrorResponse({status: 0}), 'Fallback', {
                conflictMessage: 'Account already exists.',
            }),
        ).toBe('Không thể kết nối máy chủ. Kiểm tra kết nối rồi thử lại.');
    });

    it('uses the fallback for unknown failures even with a conflict override', () => {
        expect(
            apiErrorMessage(new Error('Unexpected error'), 'Fallback', {
                conflictMessage: 'Account already exists.',
            }),
        ).toBe('Fallback');
    });
});
