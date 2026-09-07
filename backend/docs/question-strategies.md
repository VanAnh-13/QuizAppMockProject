# Question creation strategies

`Quizapp.Application.Strategies.QuizManager.Questions.IQuestionCreationStrategy`
validates the active answers for its supported question types. `AddApplication()`
registers the strategies with scoped lifetime. Strategies do not create entities
or save data; a calling workflow supplies the active answers.

| Question type | Active answer requirements |
| --- | --- |
| SingleChoice | At least two options; exactly one correct. |
| MultipleChoice | At least two options; at least one correct. |
| TrueFalse | Exactly two options; exactly one correct. Caller supplies labels. |
| FillInTheBlanks, ShortAnswer | At least one answer; all marked correct. |
| LongAnswer | References are optional; supplied references must be marked correct. |

Invalid answer shapes raise FluentValidation's `ValidationException` for `Answers`.
The nested answer DTO defaults `IsActive` to true. Its validator checks answer text;
the creation strategy checks the answer shape. Neither implements scoring.

`QuestionStrategyTests` exercises these rules directly through the registered
interface so this feature can be tested before integrating a question factory.
