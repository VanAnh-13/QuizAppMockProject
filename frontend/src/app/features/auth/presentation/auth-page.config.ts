export const AUTH_BENEFITS = [
    {
        icon: 'category',
        title: 'Đa dạng chủ đề',
        description: 'C#, .NET, SQL và Web — củng cố kiến thức theo từng chủ đề.'
    },
    {icon: 'speed', title: 'Kết quả tức thì', description: 'Xem điểm số và đáp án sau mỗi lần nộp bài.'},
    {icon: 'history_edu', title: 'Lưu lịch sử', description: 'Theo dõi kết quả và tiếp tục những bài đang làm dở.'},
] as const;

export interface AuthField {
    readonly name: 'familyName' | 'givenName' | 'email' | 'username' | 'phoneNumber' | 'dateOfBirth' | 'password' | 'confirmPassword';
    readonly label: string;
    readonly placeholder: string;
    readonly type: string;
    readonly autocomplete: string;
    readonly required: boolean;
    readonly icon?: string;
}

const USERNAME_FIELD: AuthField = {
    name: 'username', label: 'Tên đăng nhập', placeholder: 'Nhập tên đăng nhập',
    type: 'text', autocomplete: 'username', required: true, icon: 'person',
};

const PASSWORD_FIELD: AuthField = {
    name: 'password', label: 'Mật khẩu', placeholder: 'Nhập mật khẩu của bạn',
    type: 'password', autocomplete: 'current-password', required: true, icon: 'lock',
};

export const LOGIN_FIELDS: readonly AuthField[] = [USERNAME_FIELD, PASSWORD_FIELD];

export const REGISTER_FIELDS: readonly AuthField[] = [
    {
        name: 'familyName',
        label: 'Họ và tên đệm',
        placeholder: 'Nguyễn Văn',
        type: 'text',
        autocomplete: 'family-name',
        required: true
    },
    {name: 'givenName', label: 'Tên', placeholder: 'An', type: 'text', autocomplete: 'given-name', required: true},
    {
        name: 'email',
        label: 'Email',
        placeholder: 'an.nguyen@example.com',
        type: 'email',
        autocomplete: 'email',
        required: true,
        icon: 'mail'
    },
    USERNAME_FIELD,
    {
        name: 'phoneNumber',
        label: 'Số điện thoại',
        placeholder: '0912 345 678',
        type: 'tel',
        autocomplete: 'tel',
        required: false
    },
    {name: 'dateOfBirth', label: 'Ngày sinh', placeholder: '', type: 'date', autocomplete: 'bday', required: false},
    {...PASSWORD_FIELD, autocomplete: 'new-password', placeholder: 'Tối thiểu 8 ký tự'},
    {
        name: 'confirmPassword',
        label: 'Xác nhận mật khẩu',
        placeholder: 'Nhập lại mật khẩu',
        type: 'password',
        autocomplete: 'new-password',
        required: true
    },
];
