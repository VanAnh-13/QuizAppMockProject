# Role and question creation

Creation logic lives in Application. `RoleService.CreateAsync` authorizes the
caller, validates the request, checks name uniqueness and saves the new role.
`QuestionFactory` validates and builds a question graph in memory; the question
service handles authorization and persistence.

## Responsibilities

| Type / namespace | Responsibility |
| --- | --- |
| `Services.RoleManager.IRoleService` | Creates and saves a `Role` from `CreateRoleDto` after authorization, validation and name-uniqueness checks. Custom role names remain supported. |
| `Factories.QuizManager.Questions.IQuestionFactory` | Validates `CreateQuestionDto`, selects the registered strategy, and creates a `Question` with its `Answer` entities. |
| `Strategies.QuizManager.Questions.IQuestionCreationStrategy` | Defines the answer-shape rules for its supported question types. |
| `DTOs.QuizManager.Questions.CreateQuestionAnswerDto` | Carries answer text, correctness and activity when creating a question. IDs and navigation properties are supplied by the factory. |

All namespaces above start with `Quizapp.Application.`. Each public type has its
own file in the corresponding folder. Question strategies validate the part of
creation that varies by type; common ID generation and entity mapping remain in
`QuestionFactory`. Role creation has one workflow and stays in `RoleService`
without a separate factory or strategies for hardcoded role names.

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

## Create requests directly

`AddApplication()` registers the role service, question factory, strategies and
validators with scoped lifetime. The following snippets assume injected variables
named `roleService` and `questionFactory`. The role service requires an
authenticated administrator and persists the new role; the question factory only
builds the entity graph.

```csharp
using Quizapp.Application.DTOs.RoleManager;

var role = await roleService.CreateAsync(new CreateRoleDto
{
    RoleName = "Question Reviewer",
    Description = "Reviews question content",
    IsActive = true
});
```

```csharp
using Quizapp.Application.DTOs.QuizManager.Questions;
using Quizapp.Domain.Enums;

var request = new CreateQuestionDto
{
    Content = "Which keyword declares a class in C#?",
    QuestionType = QuestionType.SingleChoice,
    Level = QuestionLevel.Easy,
    IsActive = true,
    Answers =
    [
        new CreateQuestionAnswerDto { Text = "class", IsCorrect = true },
        new CreateQuestionAnswerDto { Text = "namespace", IsCorrect = false }
    ]
};

var question = questionFactory.Create(request);
```

Object initializers assign the mutable DTO properties directly. Unset properties
use DTO defaults. `RoleService` and `QuestionFactory` run FluentValidation;
`QuestionFactory` additionally applies the selected strategy. See
[DTO construction](dto-construction.md) for collection-copying guidance.
Invalid request data produces `ValidationException`. Missing strategy registration
produces `NotSupportedException`; conflicting registrations fail with
`InvalidOperationException`. `AddApplication()` can be called repeatedly without
duplicating its own registrations.

To add a new question type, implement `IQuestionCreationStrategy`, declare its
`SupportedTypes`, and register it in `AddApplication()`. The factory uses the DI
registrations rather than a switch that must be edited for every type. Add tests
for the new rules and enum coverage. Persistence changes, if any, require a
separate migration review.
