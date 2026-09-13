import { HttpErrorResponse } from '@angular/common/http';
import { computed, inject, Injectable, OnDestroy, signal } from '@angular/core';
import { apiErrorMessage } from '../../../core/api/api-error';
import {
  ATTEMPT_API,
  AttemptAnswer,
  AttemptProgress,
  AttemptQuestionDto,
  AttemptResult,
  AttemptStart,
} from '../application/attempt-api';
import { ATTEMPT_CONFIG } from '../application/attempt-config';
import { AttemptQuestion } from '../domain/quiz-attempt';

const MULTIPLE_CHOICE_TYPE = 1;
const FIRST_TEXT_QUESTION_TYPE = 4;
const SUPPORTED_QUESTION_TYPES = new Set([1, 2, 3, 4, 5, 6]);

interface Question extends AttemptQuestion {
  readonly id: string;
  readonly isText: boolean;
  readonly image: string | null;
}

type AttemptSnapshot = AttemptStart | AttemptProgress;

@Injectable()
export class QuizAttemptStore implements OnDestroy {
  readonly attemptId = signal<string | null>(null);
  readonly title = signal('Bài quiz');
  readonly questions = signal<readonly Question[]>([]);
  readonly currentQuestionNumber = signal(1);
  readonly isPaused = signal(false);
  readonly remainingSeconds = signal(0);
  readonly isLoading = signal(false);
  readonly isSaving = signal(false);
  readonly isBusy = signal(false);
  readonly dirty = signal(false);
  readonly requiresReload = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly result = signal<AttemptResult | null>(null);
  readonly markedQuestionNumbers = signal<ReadonlySet<number>>(new Set());
  readonly answers = signal<Readonly<Record<string, AttemptAnswer>>>({});
  readonly currentQuestion = computed(
    () => this.questions()[this.currentQuestionNumber() - 1] ?? null,
  );
  readonly currentAnswer = computed(() => {
    const questionId = this.currentQuestion()?.id;
    return questionId ? this.answers()[questionId] : undefined;
  });
  readonly selectedOptionIds = computed(() => this.currentAnswer()?.answerIds ?? []);
  readonly responseText = computed(() => this.currentAnswer()?.responseText ?? '');
  readonly answeredCount = computed(() => {
    return this.questions().filter((question) => this.isQuestionAnswered(question.number)).length;
  });
  readonly unansweredCount = computed(() => this.questions().length - this.answeredCount());
  readonly progressPercent = computed(() => {
    const questionCount = this.questions().length;
    return questionCount ? (this.answeredCount() / questionCount) * 100 : 0;
  });
  readonly canGoPrevious = computed(() => this.currentQuestionNumber() > 1);
  readonly canGoNext = computed(() => this.currentQuestionNumber() < this.questions().length);
  readonly canEdit = computed(() => {
    return (
      !!this.attemptId() &&
      !this.isLoading() &&
      !this.isPaused() &&
      !this.isBusy() &&
      !this.result() &&
      !this.requiresReload() &&
      this.remainingSeconds() > 0
    );
  });
  readonly formattedTime = computed(() => formatTime(this.remainingSeconds()));
  readonly saveStatus = computed(() => this.getSaveStatus());
  private readonly api = inject(ATTEMPT_API);
  private readonly config = inject(ATTEMPT_CONFIG);
  private revision = 0;
  private quizId = '';
  private editVersion = 0;
  private savedVersion = 0;
  private loadVersion = 0;
  private deadline = 0;
  private saveTimer?: ReturnType<typeof setTimeout>;
  private savingPromise: Promise<boolean> | null = null;
  private expiredSubmissionAttempted = false;
  private destroyed = false;

  async load(quizId: string, attemptId?: string | null): Promise<void> {
    const loadVersion = ++this.loadVersion;

    this.cancelScheduledSave();
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.quizId = quizId;

    try {
      const resolvedAttemptId = attemptId || (await this.findUnfinishedAttemptId(quizId));
      const requestedAt = performance.now();
      const snapshot = resolvedAttemptId
        ? await this.api.progress(resolvedAttemptId)
        : await this.api.start(quizId);

      if (this.shouldIgnoreLoad(loadVersion)) {
        return;
      }

      this.applySnapshot(snapshot, requestedAt, true);
      this.resetLoadedAttemptState();
    } catch (error) {
      if (this.shouldIgnoreLoad(loadVersion)) {
        return;
      }

      if (await this.recoverConflictedResult(attemptId, error)) {
        return;
      }

      this.errorMessage.set(
        apiErrorMessage(error, 'Không thể tải lượt làm bài. Vui lòng thử lại.'),
      );
    } finally {
      if (loadVersion === this.loadVersion) {
        this.isLoading.set(false);
      }
    }
  }

  toggleOption(optionId: string): void {
    const question = this.currentQuestion();

    if (!this.canToggleOption(question, optionId)) {
      return;
    }

    this.updateAnswer({
      questionId: question.id,
      answerIds: this.getNextOptionIds(question, optionId),
    });
  }

  setResponseText(responseText: string): void {
    const question = this.currentQuestion();

    if (!this.canEdit() || !question?.isText) {
      return;
    }

    this.updateAnswer({
      questionId: question.id,
      answerIds: [],
      responseText,
    });
  }

  isOptionSelected(optionId: string): boolean {
    return this.selectedOptionIds().includes(optionId);
  }

  isQuestionAnswered(questionNumber: number): boolean {
    const question = this.questions()[questionNumber - 1];
    const answer = question ? this.answers()[question.id] : undefined;

    return !!answer && hasAnswerContent(answer);
  }

  isQuestionMarked(questionNumber: number): boolean {
    return this.markedQuestionNumbers().has(questionNumber);
  }

  goToQuestion(questionNumber: number): void {
    const isValidQuestion =
      Number.isInteger(questionNumber) &&
      questionNumber >= 1 &&
      questionNumber <= this.questions().length;

    if (isValidQuestion) {
      this.currentQuestionNumber.set(questionNumber);
    }
  }

  previousQuestion(): void {
    this.goToQuestion(this.currentQuestionNumber() - 1);
  }

  nextQuestion(): void {
    this.goToQuestion(this.currentQuestionNumber() + 1);
  }

  toggleReview(): void {
    const questionNumber = this.currentQuestionNumber();

    this.markedQuestionNumbers.update((markedQuestions) => {
      const updatedQuestions = new Set(markedQuestions);

      if (updatedQuestions.has(questionNumber)) {
        updatedQuestions.delete(questionNumber);
      } else {
        updatedQuestions.add(questionNumber);
      }

      return updatedQuestions;
    });
  }

  async save(): Promise<boolean> {
    this.cancelScheduledSave();

    if (this.savingPromise) {
      const previousSaveSucceeded = await this.savingPromise;
      return previousSaveSucceeded ? this.save() : false;
    }

    if (!this.dirty()) {
      return true;
    }

    const attemptId = this.attemptId();

    if (!attemptId || !this.canSave()) {
      return false;
    }

    this.savingPromise = this.persistAnswers(attemptId);
    return this.savingPromise;
  }

  async togglePause(): Promise<void> {
    const attemptId = this.attemptId();

    if (!attemptId || !this.canTogglePause()) {
      return;
    }

    this.isBusy.set(true);
    this.cancelScheduledSave();

    try {
      if (this.savingPromise && !(await this.savingPromise)) {
        return;
      }

      const requestedAt = performance.now();
      const snapshot = this.isPaused()
        ? await this.api.resume(attemptId, this.revision)
        : await this.api.pause(attemptId, this.revision, this.answerList());

      this.applySnapshot(snapshot, requestedAt, true);
      this.errorMessage.set(null);
    } catch (error) {
      this.handleWriteError(error);
    } finally {
      this.isBusy.set(false);
    }
  }

  async submit(expired = false): Promise<boolean> {
    const attemptId = this.attemptId();

    if (!attemptId || !this.canSubmit()) {
      return false;
    }

    this.isBusy.set(true);
    this.cancelScheduledSave();

    try {
      if (this.savingPromise && !(await this.savingPromise) && this.requiresReload()) {
        return false;
      }

      this.updateClock();

      if (!expired && this.remainingSeconds() > 0 && !(await this.save())) {
        return false;
      }

      const result = await this.api.submit(attemptId, this.revision);
      this.acceptResult(result);

      return true;
    } catch (error) {
      if (await this.recoverResult(attemptId)) {
        return true;
      }

      this.handleWriteError(error);
      return false;
    } finally {
      this.isBusy.set(false);
    }
  }

  tick(): void {
    if (!this.shouldUpdateClock()) {
      return;
    }

    this.updateClock();

    if (this.shouldSubmitExpiredAttempt()) {
      this.expiredSubmissionAttempted = true;
      void this.submit(true);
    }
  }

  ngOnDestroy(): void {
    this.destroyed = true;
    this.cancelScheduledSave();
  }

  private async findUnfinishedAttemptId(quizId: string): Promise<string | undefined> {
    const unfinishedAttempts = await this.api.unfinished(quizId);
    return unfinishedAttempts[0]?.attemptId;
  }

  private shouldIgnoreLoad(loadVersion: number): boolean {
    return this.destroyed || loadVersion !== this.loadVersion;
  }

  private resetLoadedAttemptState(): void {
    this.result.set(null);
    this.requiresReload.set(false);
    this.expiredSubmissionAttempted = false;
  }

  private async recoverConflictedResult(
    attemptId: string | null | undefined,
    error: unknown,
  ): Promise<boolean> {
    const isConflict = error instanceof HttpErrorResponse && error.status === 409;
    return !!attemptId && isConflict && this.recoverResult(attemptId);
  }

  private canToggleOption(question: Question | null, optionId: string): question is Question {
    return (
      this.canEdit() &&
      !!question &&
      !question.isText &&
      question.options.some((option) => option.id === optionId)
    );
  }

  private getNextOptionIds(question: Question, optionId: string): readonly string[] {
    if (!question.multiple) {
      return [optionId];
    }

    const selectedOptionIds = this.selectedOptionIds();
    return selectedOptionIds.includes(optionId)
      ? selectedOptionIds.filter((id) => id !== optionId)
      : [...selectedOptionIds, optionId];
  }

  private updateAnswer(answer: AttemptAnswer): void {
    this.answers.update((answers) => ({
      ...answers,
      [answer.questionId]: answer,
    }));

    this.editVersion++;
    this.dirty.set(true);
    this.scheduleSave();
  }

  private scheduleSave(): void {
    this.cancelScheduledSave();
    this.saveTimer = setTimeout(() => {
      void this.save();
    }, this.config.saveDelayMs);
  }

  private canSave(): boolean {
    return (
      !this.requiresReload() && !this.isPaused() && !this.result() && this.remainingSeconds() > 0
    );
  }

  private async persistAnswers(attemptId: string): Promise<boolean> {
    const editVersion = this.editVersion;
    const requestedAt = performance.now();

    this.isSaving.set(true);

    try {
      const snapshot = await this.api.save(attemptId, this.revision, this.answerList());

      this.applySnapshot(snapshot, requestedAt, false);
      this.savedVersion = editVersion;
      this.dirty.set(this.editVersion !== this.savedVersion);
      this.errorMessage.set(null);

      return true;
    } catch (error) {
      this.handleWriteError(error);
      return false;
    } finally {
      this.isSaving.set(false);
      this.savingPromise = null;
    }
  }

  private answerList(): readonly AttemptAnswer[] {
    return Object.values(this.answers()).filter(hasAnswerContent);
  }

  private canTogglePause(): boolean {
    return (
      !this.isBusy() &&
      !this.isLoading() &&
      !this.result() &&
      !this.requiresReload() &&
      this.remainingSeconds() > 0
    );
  }

  private canSubmit(): boolean {
    return (
      !this.isBusy() &&
      !this.isLoading() &&
      !this.result() &&
      !this.isPaused() &&
      !this.requiresReload()
    );
  }

  private shouldUpdateClock(): boolean {
    return !this.isLoading() && !this.result() && !!this.attemptId() && !this.isPaused();
  }

  private shouldSubmitExpiredAttempt(): boolean {
    return (
      this.remainingSeconds() === 0 &&
      !this.isBusy() &&
      !this.requiresReload() &&
      !this.expiredSubmissionAttempted
    );
  }

  private applySnapshot(
    snapshot: AttemptSnapshot,
    requestedAt: number,
    replaceAnswers: boolean,
  ): void {
    this.validateSnapshot(snapshot);

    const progress = isAttemptProgress(snapshot) ? snapshot : null;
    const questions = this.mapQuestions(snapshot.quiz.questions);
    const remainingSeconds = this.calculateRemainingSeconds(snapshot, progress, requestedAt);

    this.attemptId.set(snapshot.attemptId);
    this.revision = snapshot.revision;
    this.title.set(snapshot.quiz.title);
    this.questions.set(questions);
    this.isPaused.set(!!progress?.pausedAt);
    this.setRemainingTime(remainingSeconds);

    if (replaceAnswers) {
      this.replaceAnswers(progress?.answers ?? [], questions.length);
    }
  }

  private validateSnapshot(snapshot: AttemptSnapshot): void {
    const isValid =
      snapshot.quiz?.quizId === this.quizId &&
      !!snapshot.attemptId &&
      Number.isSafeInteger(snapshot.revision) &&
      snapshot.revision >= 0 &&
      Array.isArray(snapshot.quiz.questions);

    if (!isValid) {
      throw new Error('Lượt làm bài không hợp lệ.');
    }
  }

  private mapQuestions(questions: readonly AttemptQuestionDto[]): readonly Question[] {
    return [...questions]
      .sort((first, second) => first.order - second.order)
      .map((question, index) => this.mapQuestion(question, index));
  }

  private mapQuestion(question: AttemptQuestionDto, index: number): Question {
    const isValid =
      SUPPORTED_QUESTION_TYPES.has(question.questionType) &&
      !!question.id &&
      typeof question.content === 'string' &&
      Array.isArray(question.answers);

    if (!isValid) {
      throw new Error('Loại câu hỏi chưa được hỗ trợ.');
    }

    const isText = question.questionType >= FIRST_TEXT_QUESTION_TYPE;

    return {
      id: question.id,
      number: index + 1,
      prompt: question.content,
      multiple: question.questionType === MULTIPLE_CHOICE_TYPE,
      isText,
      image: question.image ?? null,
      guidance: getQuestionGuidance(question.questionType),
      options: question.answers.map((answer, optionIndex) => ({
        id: answer.id,
        label: String.fromCharCode(65 + optionIndex),
        description: answer.text,
      })),
    };
  }

  private calculateRemainingSeconds(
    snapshot: AttemptSnapshot,
    progress: AttemptProgress | null,
    requestedAt: number,
  ): number {
    const serverSeconds = progress
      ? progress.remainingSeconds
      : (Date.parse(snapshot.expiresAt) - Date.parse(snapshot.serverTime)) / 1000;

    if (!Number.isFinite(serverSeconds)) {
      throw new Error('Thời gian từ máy chủ không hợp lệ.');
    }

    const elapsedSeconds = progress?.pausedAt ? 0 : (performance.now() - requestedAt) / 1000;
    return Math.max(0, serverSeconds - elapsedSeconds);
  }

  private setRemainingTime(remainingSeconds: number): void {
    this.deadline = performance.now() + remainingSeconds * 1000;
    this.remainingSeconds.set(Math.ceil(remainingSeconds));
  }

  private replaceAnswers(answers: readonly AttemptAnswer[], questionCount: number): void {
    this.answers.set(Object.fromEntries(answers.map((answer) => [answer.questionId, answer])));
    this.editVersion = 0;
    this.savedVersion = 0;
    this.dirty.set(false);

    const currentQuestion = Math.min(this.currentQuestionNumber(), questionCount);
    this.goToQuestion(currentQuestion);
  }

  private updateClock(): void {
    if (this.isPaused()) {
      return;
    }

    const remainingSeconds = Math.max(0, Math.ceil((this.deadline - performance.now()) / 1000));

    this.remainingSeconds.set(remainingSeconds);
  }

  private getSaveStatus(): string {
    if (this.requiresReload()) {
      return 'Cần đồng bộ lại với máy chủ';
    }

    if (this.isSaving()) {
      return 'Đang lưu đáp án…';
    }

    if (this.dirty()) {
      return 'Có thay đổi chưa lưu';
    }

    return 'Đã đồng bộ với máy chủ';
  }

  private handleWriteError(error: unknown): void {
    this.cancelScheduledSave();
    this.updateClock();

    // A lost acknowledgement may have advanced the revision. Never replay a stale draft automatically.
    const isExpiredRequest =
      error instanceof HttpErrorResponse && error.status === 400 && this.remainingSeconds() === 0;

    this.requiresReload.set(!isExpiredRequest);
    this.errorMessage.set(
      apiErrorMessage(
        error,
        isExpiredRequest
          ? 'Đã hết giờ. Chỉ các đáp án đã lưu được chấm.'
          : 'Chưa xác nhận được thao tác. Hãy tải lại trạng thái trước khi tiếp tục.',
      ),
    );
  }

  private async recoverResult(attemptId: string): Promise<boolean> {
    try {
      this.acceptResult(await this.api.result(attemptId));
      return true;
    } catch {
      // The attempt may still be running; preserve the original operation error.
      return false;
    }
  }

  private acceptResult(result: AttemptResult): void {
    if (result.quizId !== this.quizId || !Number.isFinite(result.score)) {
      throw new Error('Kết quả không hợp lệ.');
    }

    this.result.set(result);
    this.attemptId.set(result.id);
    this.title.set(result.quizTitle);
    this.dirty.set(false);
    this.requiresReload.set(false);
    this.errorMessage.set(null);
  }

  private cancelScheduledSave(): void {
    if (this.saveTimer) {
      clearTimeout(this.saveTimer);
    }

    this.saveTimer = undefined;
  }
}

function isAttemptProgress(snapshot: AttemptSnapshot): snapshot is AttemptProgress {
  return 'remainingSeconds' in snapshot;
}

function hasAnswerContent(answer: AttemptAnswer): boolean {
  return answer.answerIds.length > 0 || !!answer.responseText?.trim();
}

function getQuestionGuidance(questionType: number): string {
  if (questionType >= FIRST_TEXT_QUESTION_TYPE) {
    return 'Nhập câu trả lời của bạn.';
  }

  if (questionType === MULTIPLE_CHOICE_TYPE) {
    return 'Chọn tất cả đáp án phù hợp.';
  }

  return 'Chọn một đáp án.';
}

function formatTime(totalSeconds: number): string {
  const minutes = Math.floor(totalSeconds / 60)
    .toString()
    .padStart(2, '0');
  const seconds = (totalSeconds % 60).toString().padStart(2, '0');

  return `${minutes}:${seconds}`;
}
