import {QuizSummary} from '../app/features/quiz-explore/domain/quiz-summary';
import {QuizDetailsSnapshot} from '../app/features/quiz-details/application/quiz-details-content';
import {AttemptProgress, AttemptResult, AttemptStart} from '../app/features/quiz-attempt/application/attempt-api';
import {UserDto} from '../app/features/auth/domain/auth-contracts';

export const TEST_QUIZ_ID = '10000000-0000-0000-0000-000000000001';
export const TEST_ATTEMPT_ID = '20000000-0000-0000-0000-000000000001';
export const TEST_USER_ID = '30000000-0000-0000-0000-000000000001';

export function createTestQuizSummary(overrides: Partial<QuizSummary> = {}): QuizSummary {
    return {
        id: TEST_QUIZ_ID,
        categoryId: 'csharp',
        categoryLabel: 'Lập trình',
        title: 'Kiểm tra C# Nâng cao',
        description: 'Bài kiểm tra kiến thức về C#, async/await, LINQ.',
        questionCount: 20,
        durationMinutes: 45,
        status: 'open',
        imageUrl: null,
        ...overrides,
    };
}

export function createTestQuizDetailsSnapshot(overrides: Partial<QuizDetailsSnapshot> = {}): QuizDetailsSnapshot {
    return {
        title: 'Kiểm tra C# Nâng cao',
        description: 'Bài kiểm tra kiến thức về C#, async/await, LINQ và thiết kế hệ thống.',
        categoryLabel: 'Lập trình',
        imageUrl: null,
        metrics: [
            {icon: 'timer', label: 'Time limit', value: '45 minutes'},
            {icon: 'quiz', label: 'Số lượng questions', value: '20 questions'},
            {icon: 'grade', label: 'Passing score yêu cầu', value: '70% trở lên'},
        ],
        topics: [
            {title: 'C# Core', description: 'Cú pháp và tính năng nâng cao'},
            {title: 'LINQ', description: 'Truy vấn dữ liệu hiệu quả'},
        ],
        guidelines: [
            {title: 'Đọc kỹ', description: 'Đọc kỹ từng questions trước khi chọn đáp án.'},
            {title: 'Lưu tự động', description: 'Hệ thống tự động lưu kết quả định kỳ.'},
        ],
        formatFacts: [
            {label: 'Hình thức', value: 'Trắc nghiệm khách quan'},
            {label: 'Xáo trộn đáp án', value: 'Có'},
        ],
        ...overrides,
    };
}

export function createTestAttemptStart(overrides: Partial<AttemptStart> = {}): AttemptStart {
    return {
        attemptId: TEST_ATTEMPT_ID,
        quiz: {
            quizId: TEST_QUIZ_ID,
            title: 'Kiểm tra C# Nâng cao',
            questions: [
                {
                    id: 'q-1',
                    order: 1,
                    content: 'Từ khóa nào dùng để giải phóng tài nguyên không quản lý?',
                    questionType: 1,
                    image: null,
                    answers: [
                        {id: 'opt-1', text: 'using'},
                        {id: 'opt-2', text: 'dispose'},
                        {id: 'opt-3', text: 'finalize'},
                    ],
                },
            ],
        },
        revision: 1,
        startedAt: '2026-09-17T10:00:00Z',
        expiresAt: '2026-09-17T10:45:00Z',
        serverTime: '2026-09-17T10:00:00Z',
        ...overrides,
    };
}

export function createTestAttemptProgress(overrides: Partial<AttemptProgress> = {}): AttemptProgress {
    return {
        ...createTestAttemptStart(),
        pausedAt: null,
        lastSavedAt: '2026-09-17T10:05:00Z',
        remainingSeconds: 2400,
        answers: [],
        ...overrides,
    };
}

export function createTestAttemptResult(overrides: Partial<AttemptResult> = {}): AttemptResult {
    return {
        id: TEST_ATTEMPT_ID,
        quizId: TEST_QUIZ_ID,
        quizTitle: 'Kiểm tra C# Nâng cao',
        score: 85,
        passedScore: 70,
        submittedAt: '2026-09-17T10:30:00Z',
        ...overrides,
    };
}

export function createTestUser(overrides: Partial<UserDto> = {}): UserDto {
    return {
        id: TEST_USER_ID,
        username: 'nguyenvana',
        email: 'vana@example.com',
        fullName: 'Nguyễn Văn A',
        phoneNumber: '0901234567',
        dateOfBirth: '1995-05-15',
        avatar: null,
        isActive: true,
        createdAt: '2026-01-01T00:00:00Z',
        updatedAt: '2026-01-01T00:00:00Z',
        roles: [{id: 'role-student', roleName: 'Student', description: 'Học viên', isActive: true}],
        ...overrides,
    };
}
