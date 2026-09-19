export type SiteInformation = 'about' | 'contact' | 'login' | 'register' | 'terms' | 'privacy' | 'passwordReset';

export const SITE_NAVIGATION = [
    {id: 'about', label: 'About'},
    {id: 'contact', label: 'Contact'},
] as const;

export const SITE_INFORMATION: Readonly<
    Record<SiteInformation, { readonly title: string; readonly description: string }>
> = {
    terms: {
        title: 'Terms of service — development preview',
        description: 'QuizApp is under development. You can create an account to take quizzes and save your results. Official terms have not been published; the form acknowledgement applies only to trying this version.',
    },
    privacy: {
        title: 'Data information — development preview',
        description: 'This form sends your username, email, and profile details to the QuizApp server. Phone number and date of birth are optional. Your session is stored in your browser; choosing Remember me keeps it after you close the browser until it expires or you log out. The official privacy policy has not been published.',
    },
    passwordReset: {
        title: 'Reset password',
        description: 'Automatic password reset is not available in this version. If you forget your password, contact the administrator who provided your account.',
    },
    about: {
        title: 'Every question is a step forward',
        description:
            'Build your programming knowledge with focused quizzes. Explore C#, SQL Server, Angular, and TypeScript at your own pace. This preview includes sample content.',
    },
    contact: {
        title: 'Contact QuizApp',
        description:
            'Official contact details will be announced when QuizApp launches. In this preview, you can explore topics and read an introduction to each quiz.',
    },
    login: {
        title: 'Log in to QuizApp',
        description:
            'Log in to take quizzes and keep track of your results. You can also browse quizzes without an account.',
    },
    register: {
        title: 'Start your learning journey',
        description:
            'Create an account to save your progress and results. You can browse topics and explore quizzes before signing up.',
    },
};
