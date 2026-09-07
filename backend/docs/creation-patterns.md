# Role and question creation

Creation logic lives in Application. Factories validate requests and create new
domain entities in memory. They do not save to SQL Server, assign roles to users,
authorize callers, or expose HTTP endpoints.

## Responsibilities

| Type / namespace | Responsibility |
| --- | --- |
| `Factories.RoleManager.IRoleFactory` | Creates a `Role` from `CreateRoleDto` using its validator and a new ID. Custom role names remain supported. |
| `Factories.QuizManager.Questions.IQuestionFactory` | Validates `CreateQuestionDto`, selects the registered strategy, and creates a `Question` with its `Answer` entities. |
| `Strategies.QuizManager.Questions.IQuestionCreationStrategy` | Defines the answer-shape rules for its supported question types. |
| `DTOs.QuizManager.Questions.CreateQuestionAnswerDto` | Carries answer text, correctness and activity when creating a question. IDs and navigation properties are supplied by the factory. |

All namespaces above start with `Quizapp.Application.`. Each public type has its
own file in the corresponding folder. Question strategies validate the part of
creation that varies by type; common ID generation and entity mapping remain in
`QuestionFactory`. Role creation currently has one workflow, so it uses a factory
without artificial strategies for hardcoded role names.

## Question creation rules

These are the initial creation policies for this implementation. They are not
scoring algorithms, and do not establish an implemented quiz-taking workflow.

| Question type | Strategy | Active answer requirements |
| --- | --- | --- |
| `SingleChoice` | `SingleChoiceQuestionStrategy` | At least two options; exactly one correct. |
| `MultipleChoice` | `MultipleChoiceQuestionStrategy` | At least two options; at least one correct. |
| `TrueFalse` | `TrueFalseQuestionStrategy` | Exactly two options; exactly one correct. Labels are supplied by the caller and can be localized. |
| `FillInTheBlanks`, `ShortAnswer` | `TextQuestionStrategy` | At least one accepted answer; all active answers must be marked correct. |
| `LongAnswer` | `LongAnswerQuestionStrategy` | Reference answers are optional; any active references must be marked correct. |

`CreateQuestionAnswerDto.IsActive` defaults to `true`. Inactive answers are
preserved in the created graph but do not count toward these requirements.
The rules also apply when creating an inactive question. Every supplied answer,
including inactive ones, must have non-empty text within the existing field limit.
Text comparison, grading, partial credit and manual review are not implemented here.

The factory generates a new ID for the question and every answer. Each answer's
`QuestionId` and `QuestionNavigation` point to the newly created question. No client
answer IDs or question IDs are accepted by the nested creation DTO.

## Use with builders

Inject `IRoleFactory` and `IQuestionFactory` into the Application workflow that
needs them. `AddApplication()` registers the factories, strategies, and validators
with scoped lifetime. The following snippets assume injected variables named
`roleFactory` and `questionFactory`.

```csharp
using Quizapp.Application.DTOs.RoleManager;

var role = roleFactory.Create(new CreateRoleDto.Builder()
    .WithRoleName("Question Reviewer")
    .WithDescription("Reviews question content")
    .WithIsActive(true)
    .Build());
```

```csharp
using Quizapp.Application.DTOs.QuizManager.Questions;
using Quizapp.Domain.Enums;

var request = new CreateQuestionDto.Builder()
    .WithContent("Which keyword declares a class in C#?")
    .WithQuestionType(QuestionType.SingleChoice)
    .WithLevel(QuestionLevel.Easy)
    .WithIsActive(true)
    .WithAnswers([
        new CreateQuestionAnswerDto.Builder()
            .WithText("class").WithIsCorrect(true).Build(),
        new CreateQuestionAnswerDto.Builder()
            .WithText("namespace").WithIsCorrect(false).Build()
    ])
    .Build();

var question = questionFactory.Create(request);
```

The DTO builder assigns mutable properties and copies its internal DTO when built.
Unset properties use DTO defaults. The factories run FluentValidation;
`QuestionFactory` additionally applies the selected strategy.
Invalid request data produces `ValidationException`. Missing strategy registration
produces `NotSupportedException`; conflicting registrations fail with
`InvalidOperationException`. `AddApplication()` can be called repeatedly without
duplicating its own registrations.

To add a new question type, implement `IQuestionCreationStrategy`, declare its
`SupportedTypes`, and register it in `AddApplication()`. The factory uses the DI
registrations rather than a switch that must be edited for every type. Add tests
for the new rules and enum coverage. Persistence changes, if any, require a
separate migration review.
