# Saving, pausing, and resuming quiz attempts

The frontend runs the countdown and triggers submission. The backend persists progress and owns the timing and scoring rules. Pausing freezes the remaining time; resuming continues the same `attemptId`. There is no background job that submits abandoned attempts.

All routes require a valid bearer token. Only the attempt owner may read or modify unfinished progress, including when the caller is an administrator.

## Routes

| Method and route | Body | Behavior |
| --- | --- | --- |
| `POST /api/quizzes/{quizId}/start` | None | Create an attempt with `revision: 0`, a persisted deadline and an immutable quiz snapshot. |
| `GET /api/attempts/in-progress` | None | List the current user's unfinished attempts. Supports `pageNumber`, `pageSize`, and optional `quizId`. Expired but unsubmitted attempts remain listed with zero remaining seconds. |
| `GET /api/attempts/{attemptId}/progress` | None | Restore questions, saved answers, revision, timestamps and remaining seconds. Reading does not resume the countdown. |
| `PUT /api/attempts/{attemptId}/progress` | `SaveQuizProgressDto` | Replace the entire saved answer list while the attempt is running and before its deadline. |
| `POST /api/attempts/{attemptId}/pause` | `SaveQuizProgressDto` | Save the latest answers and freeze the clock in the same operation. |
| `POST /api/attempts/{attemptId}/resume` | `AttemptRevisionDto` | Resume a paused attempt and return its new deadline. Resuming a running attempt leaves its deadline unchanged. |
| `POST /api/attempts/{attemptId}/submit` | `AttemptRevisionDto` | Finish and grade the last saved answer list. Suitable for the frontend's timer callback. |
| `POST /api/quizzes/{quizId}/submit` | `SubmitQuizDto` | Before expiry, submit the supplied full answer list. At or after expiry, ignore supplied answers and grade only the saved draft. |
| `GET /api/attempts/{attemptId}` | None | Read a submitted result; the owner or an administrator may access it. |
| `GET /api/quiz-history` | None | Read the current user's submitted attempts. |

`AttemptRevisionDto` contains the latest revision returned by the server:

```json
{ "revision": 2 }
```

Saving and pausing use a full answer list:

```json
{
  "revision": 2,
  "answers": [
    { "questionId": "<choice-question-id>", "answerIds": ["<option-id>"] },
    { "questionId": "<text-question-id>", "responseText": "My answer" }
  ]
}
```

Omit unanswered questions. Omitting a previously answered question clears that response; an empty `answers` list clears the entire draft. A response must use valid options or non-empty text appropriate to its question type. Single-choice and true/false responses accept exactly one option. Saving does not expose scores or grading keys.

## Frontend sequence

1. Start a new attempt or find an existing one through the in-progress list. Load its progress when restoring a page or device session.
2. Keep the returned `revision`. Serialize save requests so only one write is in flight for the attempt. Update the revision after each successful write; even saving an unchanged list advances it.
3. Save answers as the user edits them. A debounce may be used, but an answer is saved only after the API acknowledges it. Do not rely solely on a page-unload handler.
4. When pausing, send the latest full answer list and revision to `/pause`. Display the paused state only after success. The response's `remainingSeconds` stays constant while paused; its old `expiresAt` is not an active deadline until resume returns a new one.
5. When resuming, send the latest revision to `/resume` and replace the local deadline with the response. Closing or reloading a page by itself does not pause time.
6. When the countdown reaches zero, stop accepting edits, wait for an in-flight save to finish, and call `/attempts/{attemptId}/submit` with the latest revision. This endpoint submits saved answers even if called slightly before the deadline. It also works after the deadline; late edits are never accepted.
7. For an early manual submission, save the latest draft then call `/attempts/{attemptId}/submit`, or send the complete answer list directly to `/quizzes/{quizId}/submit` with the latest revision.

All returned attempt timestamps use UTC. Use `serverTime` and `remainingSeconds` to initialize the countdown, then measure elapsed time locally. Reconcile with the server after reconnecting. Requests cannot choose or extend their own timestamps. Pausing can be repeated without a configured pause limit, but cannot begin at or after expiry.

## Expiry and retries

At or after the deadline, the result uses the last successfully saved answers and records `submittedAt` as the deadline. With no saved draft, the result is zero. The backend cannot receive unsent browser answers after the deadline.

If the browser closes or loses its connection, no submission occurs automatically. The draft remains in the in-progress list; the frontend can submit it when the user returns. Calling `/progress` or listing unfinished attempts does not submit them.

| Status | Meaning and recovery |
| --- | --- |
| `400` | Paused attempts cannot be edited/submitted. Expired attempts cannot be saved, paused or resumed; submit their saved draft instead. |
| `403` | Another user's unfinished attempt. |
| `404` | Attempt does not exist. |
| `409` | Revision conflict or already submitted. Reload progress before offering another edit; do not automatically replay an old answer list. If a previous submit may have succeeded, read `/api/attempts/{attemptId}`. |
| `422` | Missing/negative revision or invalid answers. Correct the request. |

The older quiz submission route still accepts a missing revision for an untouched attempt (`revision: 0`). Once an attempt has been saved, paused or resumed, a revision is required on that route too. Repeated submissions return `409` and cannot change the original result.

## Persistence and migration

Apply `20260909160949_AddAttemptPauseAndDraft` using the migration command in the [backend README](../README.md#3-apply-database-migrations). It adds draft answers, a server-only quiz snapshot, pause/save timestamps, and the revision token. Existing answers and results are retained; old attempts receive an empty draft and revision zero. A legacy attempt without a snapshot uses the current quiz until its first saved draft captures it; historical content that was never recorded cannot be reconstructed.

New attempts retain their starting questions, option text, grading keys and quiz settings if an administrator later edits, deactivates or unassigns questions. Public start/progress responses omit grading keys and model answers for text questions. Selected answers at completion still use the existing relational constraints. Do not hard-delete question/answer rows referenced by attempts; existing management flows deactivate them or remove quiz assignments.

Every draft write, pause, resume transition and submission advances a concurrency token. SQL Server detects overlapping writes in addition to the request's revision check; an unsuccessful write does not partially save answers. Rollback refuses to remove these columns while unfinished attempts exist. Rolling back after completion removes the snapshots, restoring the older result-view behavior.

Implementation references: [EF Core concurrency handling](https://learn.microsoft.com/en-us/ef/core/saving/concurrency) and [preserving UTC when reading dates](https://learn.microsoft.com/en-us/ef/core/modeling/value-conversions#specify-the-datetimekind-when-reading-dates).
