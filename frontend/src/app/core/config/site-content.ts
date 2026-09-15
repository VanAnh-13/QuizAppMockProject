export type SiteInformation = 'about' | 'contact' | 'login' | 'register' | 'terms' | 'privacy' | 'passwordReset';

export const SITE_NAVIGATION = [
    {id: 'about', label: 'Giới thiệu'},
    {id: 'contact', label: 'Liên hệ'},
] as const;

export const SITE_INFORMATION: Readonly<
    Record<SiteInformation, { readonly title: string; readonly description: string }>
> = {
    terms: {
        title: 'Điều khoản dịch vụ — bản phát triển',
        description: 'QuizApp đang trong giai đoạn phát triển. Bạn có thể tạo tài khoản để làm quiz và lưu kết quả. Điều khoản chính thức chưa được công bố; xác nhận trên biểu mẫu chỉ áp dụng cho việc trải nghiệm phiên bản này.',
    },
    privacy: {
        title: 'Thông tin dữ liệu — bản phát triển',
        description: 'Biểu mẫu gửi tên đăng nhập, email và hồ sơ bạn cung cấp đến máy chủ QuizApp. Số điện thoại và ngày sinh là tùy chọn. Phiên đăng nhập được lưu trong trình duyệt; chọn ghi nhớ sẽ giữ phiên sau khi đóng trình duyệt cho đến khi hết hạn hoặc đăng xuất. Chính sách bảo mật chính thức chưa được công bố.',
    },
    passwordReset: {
        title: 'Đặt lại mật khẩu',
        description: 'QuizApp chưa hỗ trợ đặt lại mật khẩu tự động trong phiên bản này. Nếu quên mật khẩu, hãy liên hệ người quản trị đã cung cấp tài khoản cho bạn.',
    },
    about: {
        title: 'Mỗi câu hỏi, một bước tiến',
        description:
            'QuizApp giúp bạn củng cố kiến thức lập trình bằng các bài quiz có chủ đề rõ ràng. Khám phá C#, SQL Server, Angular và TypeScript theo nhịp học của bạn. Đây là bản trải nghiệm thiết kế với dữ liệu mẫu.',
    },
    contact: {
        title: 'Liên hệ với QuizApp',
        description:
            'Kênh liên hệ chính thức sẽ được công bố khi QuizApp ra mắt. Trong bản trải nghiệm này, bạn có thể khám phá chủ đề và xem nội dung giới thiệu của từng bài quiz.',
    },
    login: {
        title: 'Đăng nhập vào QuizApp',
        description:
            'Đăng nhập sẽ có khi QuizApp ra mắt. Hiện tại, bạn có thể khám phá các bài quiz mẫu và xem chi tiết mà không cần tài khoản.',
    },
    register: {
        title: 'Bắt đầu hành trình học tập',
        description:
            'Tính năng tạo tài khoản chưa có trong bản trải nghiệm này. Bạn vẫn có thể tìm kiếm, chọn chủ đề và khám phá các bài quiz mẫu.',
    },
};
