# DTO builders

Each DTO contains its own nested `public sealed class Builder`.
Both types live in the existing `Quizapp.Application.DTOs.<Feature>` namespace
and the same DTO file. Builders require no additional package or DI registration.

DTOs use mutable `get; set;` properties without `required` or `init`. Each builder
holds one private DTO and its `With...` methods assign that DTO's properties,
without maintaining a second set of fields.

## Create a quiz request

```csharp
using Quizapp.Application.DTOs.QuizManager.Quizzes;

var request = new CreateQuizDto.Builder()
    .WithTitle("C# basics")
    .WithDuration(30)
    .WithPassedScore(7)
    .WithIsActive(true)
    .Build();
```

Every DTO property has a corresponding `With<Property>(value)` method.
Methods return the same builder, so calls can be chained in any order.
Calling a method again replaces the previous value.

## Nested DTOs

```csharp
using Quizapp.Application.DTOs.Authentication;
using Quizapp.Application.DTOs.UserManager;

// username, email and password come from the caller.
var profile = new UserProfileDto.Builder()
    .WithFullName("Student Name")
    .Build();

var request = new RegisterDto.Builder()
    .WithUsername(username)
    .WithEmail(email)
    .WithPassword(password)
    .WithConfirmPassword(password)
    .WithProfile(profile)
    .Build();
```

## Behavior

- `Build()` creates a new DTO on each call. It does not reset the builder.
- Unset properties use their DTO defaults. Non-nullable strings start empty;
  non-nullable nested DTOs are initialized with `new()`. Value types normally
  default to `0` or `false`, and explicit defaults such as answer activity are retained.
- Non-nullable reference values and collection arguments reject `null` with
  `ArgumentNullException` in their `With...` methods.
- Optional properties retain the DTO defaults. Nullable properties can be cleared
  by passing `null`. `AddQuestionToQuizDto.Order` defaults to `QuizQuestion.FirstOrder`.
- Collection methods accept `IEnumerable<T>` and copy it immediately; `Build()`
  copies the collection again. Nested DTO objects and collection elements are
  shared references, not deep copies. Builders are mutable and are not thread-safe.
- `Build()` uses `MemberwiseClone()` to copy the internal DTO, including inherited
  properties, and separately copies collections. Callers can edit the result's
  top-level properties without changing the builder's internal DTO.
- Builders do not check required-value presence. Use the existing FluentValidation
  validators for business rules such as a positive duration, valid email, and
  matching password confirmation. A successful `Build()` does not imply a valid request.
- JSON with missing fields now deserializes to these defaults; run validation
  before processing requests. Missing `0`/`false` values cannot be distinguished
  from explicitly supplied `0`/`false` values by these non-nullable properties.
- DTO properties, object initializers, and JSON serialization/deserialization
  remain supported. Nested builder classes are not DTO instance properties and
  are not added to JSON responses.

`QuizAttemptDetailDto.Builder` also supports the properties inherited from
`QuizAttemptDto`. Its `new` modifier distinguishes it from the base DTO's nested
builder, and its `Build()` returns `QuizAttemptDetailDto`.

Behavior is covered by `tests/Quizapp.Tests/Application/DtoBuilderTests.cs`.
