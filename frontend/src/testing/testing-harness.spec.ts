import {MockApiClient} from './mock-api-client';
import {MockAuthSession} from './mock-auth-session';
import {MockConfirmationService} from './mock-confirmation';
import {MockErrorNotificationService} from './mock-error-notification';
import {
    createTestAttemptProgress,
    createTestAttemptResult,
    createTestAttemptStart,
    createTestQuizDetailsSnapshot,
    createTestQuizSummary,
    createTestUser,
} from './fixtures';

describe('Agent Testing Harness', () => {
    describe('Fixtures', () => {
        it('creates valid quiz summary with defaults and overrides', () => {
            const summary = createTestQuizSummary({title: 'Custom Title'});
            expect(summary.title).toBe('Custom Title');
            expect(summary.status).toBe('open');
            expect(summary.categoryId).toBe('csharp');
        });

        it('creates valid quiz details snapshot', () => {
            const details = createTestQuizDetailsSnapshot();
            expect(details.metrics.length).toBeGreaterThan(0);
            expect(details.topics.length).toBeGreaterThan(0);
        });

        it('creates valid attempt models', () => {
            const start = createTestAttemptStart();
            expect(start.quiz.questions.length).toBe(1);

            const progress = createTestAttemptProgress();
            expect(progress.remainingSeconds).toBe(2400);

            const result = createTestAttemptResult();
            expect(result.score).toBe(85);
        });

        it('creates valid test user', () => {
            const user = createTestUser({username: 'special_user'});
            expect(user.username).toBe('special_user');
            expect(user.roles.length).toBe(1);
        });
    });

    describe('MockApiClient', () => {
        it('supports mocking and resetting methods', async () => {
            const api = new MockApiClient();
            api.get.mockResolvedValue({data: 'ok'});

            const res = await api.get('/test');
            expect(res).toEqual({data: 'ok'});
            expect(api.get).toHaveBeenCalledWith('/test');

            api.reset();
            expect(api.get).not.toHaveBeenCalled();
        });
    });

    describe('MockAuthSession', () => {
        it('manages authenticated state transitions', () => {
            const session = new MockAuthSession();
            expect(session.isAuthenticated()).toBe(false);

            session.setAuthenticated({fullName: 'Test User'});
            expect(session.isAuthenticated()).toBe(true);
            expect(session.user()?.fullName).toBe('Test User');
            expect(session.token()).toBe('mock-jwt-token');

            session.setAnonymous();
            expect(session.isAuthenticated()).toBe(false);
            expect(session.user()).toBeNull();
            expect(session.token()).toBeNull();
        });
    });

    describe('MockConfirmationService', () => {
        it('supports auto-response configuration', async () => {
            const confirmation = new MockConfirmationService();
            confirmation.setAutoResponse(true);

            const accepted = await confirmation.confirm({
                title: 'Confirm Title',
                message: 'Confirm Message',
            });
            expect(accepted).toBe(true);
            expect(confirmation.lastOptions?.title).toBe('Confirm Title');

            confirmation.setAutoResponse(false);
            const rejected = await confirmation.confirm({
                title: 'Confirm Title 2',
                message: 'Confirm Message 2',
            });
            expect(rejected).toBe(false);
        });
    });

    describe('MockErrorNotificationService', () => {
        it('records shown messages and checks presence', () => {
            const errors = new MockErrorNotificationService();
            errors.show('Server connection lost');

            expect(errors.notifications().length).toBe(1);
            expect(errors.hasMessage('connection lost')).toBe(true);
            expect(errors.hasMessage('unexpected')).toBe(false);

            errors.clear();
            expect(errors.notifications().length).toBe(0);
            expect(errors.messages.length).toBe(0);
        });
    });
});
