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
    name: 'username', label: 'Username', placeholder: 'Enter your username',
    type: 'text', autocomplete: 'username', required: true, icon: 'person',
};

const PASSWORD_FIELD: AuthField = {
    name: 'password', label: 'Password', placeholder: 'Enter your password',
    type: 'password', autocomplete: 'current-password', required: true, icon: 'lock',
};

export const LOGIN_FIELDS: readonly AuthField[] = [USERNAME_FIELD, PASSWORD_FIELD];

export const REGISTER_FIELDS: readonly AuthField[] = [
    {
        name: 'familyName',
        label: 'Family name',
        placeholder: 'Smith',
        type: 'text',
        autocomplete: 'family-name',
        required: true
    },
    {
        name: 'givenName',
        label: 'Given name',
        placeholder: 'Alex',
        type: 'text',
        autocomplete: 'given-name',
        required: true
    },
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
        label: 'Phone number',
        placeholder: '0912 345 678',
        type: 'tel',
        autocomplete: 'tel',
        required: false
    },
    {name: 'dateOfBirth', label: 'Date of birth', placeholder: '', type: 'date', autocomplete: 'bday', required: false},
    {...PASSWORD_FIELD, autocomplete: 'new-password', placeholder: 'At least 8 characters'},
    {
        name: 'confirmPassword',
        label: 'Confirm password',
        placeholder: 'Enter your password again',
        type: 'password',
        autocomplete: 'new-password',
        required: true
    },
];
