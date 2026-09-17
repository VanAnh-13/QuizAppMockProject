# DTO construction

Create DTOs with C# object initializers. DTO properties remain mutable, and JSON
serialization, property names, nullability and default values are unchanged.

```csharp
using Quizapp.Application.DTOs.QuizManager.Quizzes;

var request = new CreateQuizDto
{
    Title = "C# basics",
    Duration = 30,
    PassedScore = 7,
    IsActive = true
};
```

Nested DTOs and collections use the same syntax:

```csharp
using Quizapp.Application.DTOs.Authentication;
using Quizapp.Application.DTOs.UserManager;

// username, email and password come from the caller.
var request = new RegisterDto
{
    Username = username,
    Email = email,
    Password = password,
    ConfirmPassword = password,
    Profile = new UserProfileDto { FullName = "Student Name" }
};
```

Construction does not validate a request. Application services and the question
factory use the existing FluentValidation validators before processing it.
Missing JSON fields still use DTO defaults and must pass request validation.

The nested `Builder` API has been removed. Replace its `With...().Build()` calls
with object initializers. Initializers assign collection and nested-object
references directly; use a new collection such as `Answers = [.. answers]` when
the caller needs an independent collection. DTO contract and validation tests
cover the HTTP-facing behavior.
