import {HttpErrorResponse} from '@angular/common/http';

export function apiErrorMessage(
    error: unknown,
    fallback: string,
    options: { conflictMessage?: string } = {},
): string {
    if (!(error instanceof HttpErrorResponse)) return fallback;

    switch (error.status) {
        case 0:
            return 'Không thể kết nối máy chủ. Kiểm tra kết nối rồi thử lại.';
        case 401:
            return 'Vui lòng đăng nhập lại để tiếp tục.';
        case 403:
            return 'Tài khoản của bạn không có quyền truy cập nội dung này.';
        case 404:
            return 'Không tìm thấy quiz hoặc lượt làm bài này.';
        case 409:
            return (
                options.conflictMessage ??
                'Dữ liệu đã thay đổi ở phiên khác hoặc bài đã được nộp. Hãy tải lại trạng thái từ máy chủ.'
            );
        case 422:
            return 'Thông tin gửi lên chưa hợp lệ. Vui lòng kiểm tra và thử lại.';
        default:
            return fallback;
    }
}
