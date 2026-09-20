using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Quizapp.Domain.Entities;
using Quizapp.Domain.Enums;

namespace Quizapp.Infrastructure.Persistence;

public static class SampleQuizDataSeeder
{
    private static readonly DateTime SeedTimestamp = new(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

    // Deterministic IDs for fresh rows; existing rows are resolved by their unique natural keys
    // (RoleName, Username, Email) and reused so seeding never violates the unique indexes.
    public static readonly Guid RoleAdminId = Guid.Parse("a0000000-0000-0000-0000-000000000001");
    public static readonly Guid RoleUserId = Guid.Parse("a0000000-0000-0000-0000-000000000002");
    public static readonly Guid DemoUserId = Guid.Parse("b0000000-0000-0000-0000-000000000001");
    public static readonly Guid DemoAdminId = Guid.Parse("b0000000-0000-0000-0000-000000000002");

    // 30 Sample Quizzes
    public static readonly Guid Quiz1CsharpOopId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    public static readonly Guid Quiz2CsharpAdvId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    public static readonly Guid Quiz3SqlTsqlId = Guid.Parse("10000000-0000-0000-0000-000000000003");
    public static readonly Guid Quiz4SqlDesignId = Guid.Parse("10000000-0000-0000-0000-000000000004");
    public static readonly Guid Quiz5AngularCompId = Guid.Parse("10000000-0000-0000-0000-000000000005");
    public static readonly Guid Quiz6AngularRouteId = Guid.Parse("10000000-0000-0000-0000-000000000006");
    public static readonly Guid Quiz7TsBasicsId = Guid.Parse("10000000-0000-0000-0000-000000000007");
    public static readonly Guid Quiz8TsAdvancedId = Guid.Parse("10000000-0000-0000-0000-000000000008");
    public static readonly Guid Quiz9ApiRestId = Guid.Parse("10000000-0000-0000-0000-000000000009");
    public static readonly Guid Quiz10ApiSecurityId = Guid.Parse("10000000-0000-0000-0000-000000000010");
    public static readonly Guid Quiz11EfCoreModelingId = Guid.Parse("10000000-0000-0000-0000-000000000011");
    public static readonly Guid Quiz12EfCoreOptimizationId = Guid.Parse("10000000-0000-0000-0000-000000000012");
    public static readonly Guid Quiz13CsharpLinqId = Guid.Parse("10000000-0000-0000-0000-000000000013");
    public static readonly Guid Quiz14AspNetCoreDiId = Guid.Parse("10000000-0000-0000-0000-000000000014");
    public static readonly Guid Quiz15SqlTransactionsId = Guid.Parse("10000000-0000-0000-0000-000000000015");
    public static readonly Guid Quiz16AngularChangeDetectionId = Guid.Parse("10000000-0000-0000-0000-000000000016");
    public static readonly Guid Quiz17RxjsOperatorsId = Guid.Parse("10000000-0000-0000-0000-000000000017");
    public static readonly Guid Quiz18GitVersionControlId = Guid.Parse("10000000-0000-0000-0000-000000000018");
    public static readonly Guid Quiz19CleanCodeSolidId = Guid.Parse("10000000-0000-0000-0000-000000000019");
    public static readonly Guid Quiz20DesignPatternsGofId = Guid.Parse("10000000-0000-0000-0000-000000000020");
    public static readonly Guid Quiz21WebSecurityOwaspId = Guid.Parse("10000000-0000-0000-0000-000000000021");
    public static readonly Guid Quiz22DockerContainersId = Guid.Parse("10000000-0000-0000-0000-000000000022");
    public static readonly Guid Quiz23UnitTestingTddId = Guid.Parse("10000000-0000-0000-0000-000000000023");
    public static readonly Guid Quiz24MicroservicesPatternsId = Guid.Parse("10000000-0000-0000-0000-000000000024");
    public static readonly Guid Quiz25RestfulApiConventionsId = Guid.Parse("10000000-0000-0000-0000-000000000025");
    public static readonly Guid Quiz26CiCdPipelinesId = Guid.Parse("10000000-0000-0000-0000-000000000026");
    public static readonly Guid Quiz27WebPerformanceId = Guid.Parse("10000000-0000-0000-0000-000000000027");
    public static readonly Guid Quiz28AspNetCoreMiddlewareId = Guid.Parse("10000000-0000-0000-0000-000000000028");
    public static readonly Guid Quiz29AngularFormsValidationId = Guid.Parse("10000000-0000-0000-0000-000000000029");
    public static readonly Guid Quiz30TsGenericsConstraintsId = Guid.Parse("10000000-0000-0000-0000-000000000030");

    public static readonly Guid[] SampleQuizIds =
    [
        Quiz1CsharpOopId, Quiz2CsharpAdvId, Quiz3SqlTsqlId, Quiz4SqlDesignId, Quiz5AngularCompId,
        Quiz6AngularRouteId, Quiz7TsBasicsId, Quiz8TsAdvancedId, Quiz9ApiRestId, Quiz10ApiSecurityId,
        Quiz11EfCoreModelingId, Quiz12EfCoreOptimizationId, Quiz13CsharpLinqId, Quiz14AspNetCoreDiId, Quiz15SqlTransactionsId,
        Quiz16AngularChangeDetectionId, Quiz17RxjsOperatorsId, Quiz18GitVersionControlId, Quiz19CleanCodeSolidId, Quiz20DesignPatternsGofId,
        Quiz21WebSecurityOwaspId, Quiz22DockerContainersId, Quiz23UnitTestingTddId, Quiz24MicroservicesPatternsId, Quiz25RestfulApiConventionsId,
        Quiz26CiCdPipelinesId, Quiz27WebPerformanceId, Quiz28AspNetCoreMiddlewareId, Quiz29AngularFormsValidationId, Quiz30TsGenericsConstraintsId
    ];

    public static async Task SeedAsync(QuizAppDbContext db, CancellationToken cancellationToken = default)
    {
        await SeedRolesAndUserAsync(db, cancellationToken);
        await SeedQuizzesAndQuestionsAsync(db, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedRolesAndUserAsync(QuizAppDbContext db, CancellationToken cancellationToken)
    {
        var adminRole = await db.Roles
            .FirstOrDefaultAsync(role => role.RoleName == "Admin", cancellationToken);
        if (adminRole is null)
        {
            adminRole = new Role
            {
                Id = RoleAdminId,
                RoleName = "Admin",
                Description = "System administrator",
                IsActive = true
            };
            db.Roles.Add(adminRole);
        }
        else
        {
            if (adminRole.Description == "Quản trị viên hệ thống")
                adminRole.Description = "System administrator";
            adminRole.IsActive = true;
        }

        var userRole = await db.Roles
            .FirstOrDefaultAsync(role => role.RoleName == "User", cancellationToken);
        if (userRole is null)
        {
            userRole = new Role
            {
                Id = RoleUserId,
                RoleName = "User",
                Description = "Quiz participant",
                IsActive = true
            };
            db.Roles.Add(userRole);
        }
        else
        {
            if (userRole.Description == "Người dùng tham gia bài thi")
                userRole.Description = "Quiz participant";
            userRole.IsActive = true;
        }

        var hasher = new PasswordHasher<object>();
        var passwordHash = hasher.HashPassword(new object(), "Password123!");

        var demoUser = await EnsureDemoAccountAsync(
            db,
            username: "demo_user",
            email: "demo@quizapp.local",
            fallbackId: DemoUserId,
            fullName: "Demo Learner",
            securityStamp: Guid.Parse("c0000000-0000-0000-0000-000000000001"),
            passwordHash,
            cancellationToken);

        var demoAdmin = await EnsureDemoAccountAsync(
            db,
            username: "demo_admin",
            email: "admin@quizapp.local",
            fallbackId: DemoAdminId,
            fullName: "Demo Administrator",
            securityStamp: Guid.Parse("c0000000-0000-0000-0000-000000000002"),
            passwordHash,
            cancellationToken);

        await EnsureUserRoleAsync(db, demoUser.Id, userRole.Id, cancellationToken);
        await EnsureUserRoleAsync(db, demoAdmin.Id, adminRole.Id, cancellationToken);
    }

    private static async Task<User> EnsureDemoAccountAsync(
        QuizAppDbContext db,
        string username,
        string email,
        Guid fallbackId,
        string fullName,
        Guid securityStamp,
        string passwordHash,
        CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(item => item.Username == username, cancellationToken)
                   ?? await db.Users.FirstOrDefaultAsync(item => item.Email == email, cancellationToken);
        if (user is not null)
        {
            if (user.FullName is "Học viên Demo" or "Quản trị viên Demo")
                user.FullName = fullName;
            return user;
        }

        user = new User
        {
            Id = fallbackId,
            Username = username,
            Email = email,
            Password = passwordHash,
            FullName = fullName,
            Status = UserStatus.Active,
            SecurityStamp = securityStamp,
            CreateAt = SeedTimestamp,
            UpdateAt = SeedTimestamp
        };
        db.Users.Add(user);
        return user;
    }

    private static async Task EnsureUserRoleAsync(
        QuizAppDbContext db,
        Guid userId,
        Guid roleId,
        CancellationToken cancellationToken)
    {
        var assigned = await db.UserRoles.AnyAsync(
            assignment => assignment.UserId == userId && assignment.RoleId == roleId,
            cancellationToken);
        if (assigned) return;

        db.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });
    }

    private static async Task SeedQuizzesAndQuestionsAsync(QuizAppDbContext db, CancellationToken cancellationToken)
    {
        var quizDefs = BuildSampleQuizzes();

        for (var quizIndex = 0; quizIndex < quizDefs.Length; quizIndex++)
        {
            var def = quizDefs[quizIndex];
            var existingQuiz = await db.Quizzes
                .Include(q => q.QuizQuestions)
                .FirstOrDefaultAsync(q => q.Id == def.Quiz.Id, cancellationToken);

            Quiz targetQuiz;
            if (existingQuiz is null)
            {
                targetQuiz = def.Quiz;
                db.Quizzes.Add(targetQuiz);
            }
            else
            {
                targetQuiz = existingQuiz;
                targetQuiz.Title = def.Quiz.Title;
                targetQuiz.Description = def.Quiz.Description;
                targetQuiz.Duration = def.Quiz.Duration;
                targetQuiz.PassedScore = def.Quiz.PassedScore;
                targetQuiz.IsActive = true;
                targetQuiz.UpdateAt = SeedTimestamp;
            }

            var order = 1;
            var assignedQuestionIds = new HashSet<Guid>();

            foreach (var qData in def.Questions)
            {
                assignedQuestionIds.Add(qData.Question.Id);

                var existingQuestion = await db.Questions
                    .Include(q => q.Answers)
                    .FirstOrDefaultAsync(q => q.Id == qData.Question.Id, cancellationToken);

                if (existingQuestion is null)
                {
                    db.Questions.Add(qData.Question);
                    foreach (var answer in qData.Answers)
                    {
                        db.Answers.Add(answer);
                    }
                }
                else
                {
                    existingQuestion.Content = qData.Question.Content;
                    existingQuestion.QuestionType = qData.Question.QuestionType;
                    existingQuestion.Level = qData.Question.Level;
                    existingQuestion.IsActive = true;

                    var validAnswerIds = qData.Answers.Select(a => a.Id).ToHashSet();
                    foreach (var oldAnswer in existingQuestion.Answers.Where(a => !validAnswerIds.Contains(a.Id)).ToList())
                    {
                        db.Answers.Remove(oldAnswer);
                    }

                    foreach (var newAnswer in qData.Answers)
                    {
                        var existingAnswer = existingQuestion.Answers.FirstOrDefault(a => a.Id == newAnswer.Id);
                        if (existingAnswer is null)
                        {
                            db.Answers.Add(newAnswer);
                        }
                        else
                        {
                            db.Entry(existingAnswer).CurrentValues.SetValues(newAnswer);
                        }
                    }
                }

                var existingQq = targetQuiz.QuizQuestions.FirstOrDefault(qq => qq.QuestionId == qData.Question.Id);
                if (existingQq is null)
                {
                    var quizQuestionId = Guid.Parse($"40000000-0000-0000-{(quizIndex + 1):D4}-{order:D12}");
                    db.QuizQuestions.Add(new QuizQuestion
                    {
                        Id = quizQuestionId,
                        QuizId = targetQuiz.Id,
                        QuestionId = qData.Question.Id,
                        Order = order++
                    });
                }
                else
                {
                    existingQq.Order = order++;
                }
            }

            // Clean up any stale question associations on this sample quiz
            var staleQqs = targetQuiz.QuizQuestions
                .Where(qq => !assignedQuestionIds.Contains(qq.QuestionId))
                .ToList();

            foreach (var staleQq in staleQqs)
            {
                db.QuizQuestions.Remove(staleQq);
            }
        }
    }

    private sealed record QuestionSeedData(Question Question, Answer[] Answers);
    private sealed record QuizSeedData(Quiz Quiz, QuestionSeedData[] Questions);

    private static QuizSeedData[] BuildSampleQuizzes()
    {
        return
        [
            // 1. C# OOP
            new QuizSeedData(
                new Quiz
                {
                    Id = Quiz1CsharpOopId,
                    Title = "C# Fundamentals & Object-Oriented Programming",
                    Description = "Comprehensive assessment of core OOP principles, classes, interfaces, inheritance, polymorphism, and encapsulation in C#.",
                    Duration = 20,
                    PassedScore = 70.0,
                    IsActive = true,
                    CreateAt = SeedTimestamp,
                    UpdateAt = SeedTimestamp
                },
                [
                    CreateQuestion(
                        1,
                        "Which keyword is used in C# to prevent a class from being inherited by other classes?",
                        QuestionType.SingleChoice,
                        QuestionLevel.Easy,
                        [
                            ("abstract", false),
                            ("sealed", true),
                            ("static", false),
                            ("readonly", false)
                        ]),
                    CreateQuestion(
                        2,
                        "Which of the following are core pillars of Object-Oriented Programming (OOP)?",
                        QuestionType.MultipleChoice,
                        QuestionLevel.Medium,
                        [
                            ("Encapsulation", true),
                            ("Inheritance", true),
                            ("Polymorphism", true),
                            ("Ahead-of-Time Compilation", false)
                        ]),
                    CreateQuestion(
                        3,
                        "In C#, a class can implement multiple interfaces simultaneously.",
                        QuestionType.TrueFalse,
                        QuestionLevel.Easy,
                        [
                            ("True", true),
                            ("False", false)
                        ])
                ]
            ),

            // 2. C# Advanced
            new QuizSeedData(
                new Quiz
                {
                    Id = Quiz2CsharpAdvId,
                    Title = "C# Advanced: Asynchronous Programming & CLR",
                    Description = "Test deep knowledge of async/await concurrency patterns, Task-based asynchronous programming, Garbage Collection, and CLR memory management.",
                    Duration = 25,
                    PassedScore = 75.0,
                    IsActive = true,
                    CreateAt = SeedTimestamp,
                    UpdateAt = SeedTimestamp
                },
                [
                    CreateQuestion(
                        4,
                        "What is the primary effect of the 'await' keyword in C# when applied to an uncompleted Task?",
                        QuestionType.SingleChoice,
                        QuestionLevel.Medium,
                        [
                            ("It yields execution back to the caller without blocking the calling thread until the Task completes", true),
                            ("It blocks the calling thread synchronously like Thread.Sleep", false),
                            ("It creates a brand-new OS background thread directly", false),
                            ("It cancels the Task if execution exceeds five seconds", false)
                        ]),
                    CreateQuestion(
                        5,
                        "Which of the following LINQ operators utilize deferred (lazy) execution?",
                        QuestionType.MultipleChoice,
                        QuestionLevel.Hard,
                        [
                            ("Where(...)", true),
                            ("Select(...)", true),
                            ("ToList()", false),
                            ("Count()", false)
                        ]),
                    CreateQuestion(
                        6,
                        "In C#, struct is a value type and instances allocated locally inside methods are typically stored on the stack.",
                        QuestionType.TrueFalse,
                        QuestionLevel.Medium,
                        [
                            ("True", true),
                            ("False", false)
                        ])
                ]
            ),

            // 3. SQL Server T-SQL
            new QuizSeedData(
                new Quiz
                {
                    Id = Quiz3SqlTsqlId,
                    Title = "SQL Server: T-SQL Querying & Data Manipulation",
                    Description = "Assess proficiency in writing production-grade T-SQL queries including joins, aggregations, grouping, and subqueries.",
                    Duration = 20,
                    PassedScore = 70.0,
                    IsActive = true,
                    CreateAt = SeedTimestamp,
                    UpdateAt = SeedTimestamp
                },
                [
                    CreateQuestion(
                        7,
                        "Which SQL clause is used to filter aggregated groups created by a GROUP BY clause?",
                        QuestionType.SingleChoice,
                        QuestionLevel.Easy,
                        [
                            ("WHERE", false),
                            ("HAVING", true),
                            ("ORDER BY", false),
                            ("FILTER", false)
                        ]),
                    CreateQuestion(
                        8,
                        "What is the primary difference between UNION and UNION ALL operators in T-SQL?",
                        QuestionType.SingleChoice,
                        QuestionLevel.Medium,
                        [
                            ("UNION eliminates duplicate rows, whereas UNION ALL retains all duplicates", true),
                            ("UNION ALL eliminates duplicate rows, whereas UNION retains all duplicates", false),
                            ("UNION operates only on integer columns", false),
                            ("UNION ALL automatically sorts results in ascending order", false)
                        ]),
                    CreateQuestion(
                        9,
                        "Which of the following are valid SQL aggregate functions?",
                        QuestionType.MultipleChoice,
                        QuestionLevel.Medium,
                        [
                            ("COUNT()", true),
                            ("SUM()", true),
                            ("AVG()", true),
                            ("CONCAT()", false)
                        ])
                ]
            ),

            // 4. SQL Server Design & Indexing
            new QuizSeedData(
                new Quiz
                {
                    Id = Quiz4SqlDesignId,
                    Title = "SQL Server: Database Design & Index Optimization",
                    Description = "Master relational database schema normalization, clustered versus non-clustered indexes, and query performance tuning.",
                    Duration = 25,
                    PassedScore = 70.0,
                    IsActive = true,
                    CreateAt = SeedTimestamp,
                    UpdateAt = SeedTimestamp
                },
                [
                    CreateQuestion(
                        10,
                        "How many clustered indexes can a single physical table have in SQL Server?",
                        QuestionType.SingleChoice,
                        QuestionLevel.Easy,
                        [
                            ("1", true),
                            ("2", false),
                            ("249", false),
                            ("Unlimited", false)
                        ]),
                    CreateQuestion(
                        11,
                        "Which properties comprise the ACID guarantees in relational database management systems?",
                        QuestionType.MultipleChoice,
                        QuestionLevel.Hard,
                        [
                            ("Atomicity", true),
                            ("Consistency", true),
                            ("Isolation", true),
                            ("Durability", true)
                        ]),
                    CreateQuestion(
                        12,
                        "A non-clustered index physically re-orders the underlying table data rows on disk.",
                        QuestionType.TrueFalse,
                        QuestionLevel.Medium,
                        [
                            ("True", false),
                            ("False", true)
                        ])
                ]
            ),

            // 5. Angular Components & Signals
            new QuizSeedData(
                new Quiz
                {
                    Id = Quiz5AngularCompId,
                    Title = "Angular: Components, Signals & Dependency Injection",
                    Description = "Explore modern Angular component architecture, reactive Signals, standalone components, and hierarchical dependency injection.",
                    Duration = 20,
                    PassedScore = 70.0,
                    IsActive = true,
                    CreateAt = SeedTimestamp,
                    UpdateAt = SeedTimestamp
                },
                [
                    CreateQuestion(
                        13,
                        "In modern Angular, which function is used to declare a reactive writable signal?",
                        QuestionType.SingleChoice,
                        QuestionLevel.Easy,
                        [
                            ("signal()", true),
                            ("computed()", false),
                            ("effect()", false),
                            ("state()", false)
                        ]),
                    CreateQuestion(
                        14,
                        "When an Angular service is provided with '@Injectable({ providedIn: 'root' })', what is its lifetime scope?",
                        QuestionType.SingleChoice,
                        QuestionLevel.Medium,
                        [
                            ("Singleton instance shared across the entire application", true),
                            ("A new instance per component injection", false),
                            ("A new instance per route navigation", false),
                            ("Scoped only to the declaring feature module", false)
                        ]),
                    CreateQuestion(
                        15,
                        "Which of the following functions are part of the core Angular Signals reactivity primitives?",
                        QuestionType.MultipleChoice,
                        QuestionLevel.Medium,
                        [
                            ("signal()", true),
                            ("computed()", true),
                            ("effect()", true),
                            ("fromEvent()", false)
                        ])
                ]
            ),

            // 6. Angular Routing & Forms
            new QuizSeedData(
                new Quiz
                {
                    Id = Quiz6AngularRouteId,
                    Title = "Angular: Routing, Guards & Reactive Forms",
                    Description = "Evaluate navigation pipelines, functional route guards, lazy loading, and robust form validation using ReactiveFormsModule.",
                    Duration = 25,
                    PassedScore = 70.0,
                    IsActive = true,
                    CreateAt = SeedTimestamp,
                    UpdateAt = SeedTimestamp
                },
                [
                    CreateQuestion(
                        16,
                        "Which Angular router guard is most commonly used to check whether a user is authenticated before allowing route entry?",
                        QuestionType.SingleChoice,
                        QuestionLevel.Medium,
                        [
                            ("canActivate", true),
                            ("canDeactivate", false),
                            ("resolve", false),
                            ("canMatchChildren", false)
                        ]),
                    CreateQuestion(
                        17,
                        "Which RxJS operator cancels the in-flight inner observable when a new value is emitted by the source observable?",
                        QuestionType.SingleChoice,
                        QuestionLevel.Medium,
                        [
                            ("switchMap", true),
                            ("mergeMap", false),
                            ("concatMap", false),
                            ("exhaustMap", false)
                        ]),
                    CreateQuestion(
                        18,
                        "In Angular Reactive Forms, a FormGroup tracks the value and validation status of a group of FormControl instances.",
                        QuestionType.TrueFalse,
                        QuestionLevel.Easy,
                        [
                            ("True", true),
                            ("False", false)
                        ])
                ]
            ),

            // 7. TypeScript Basics & Generics
            new QuizSeedData(
                new Quiz
                {
                    Id = Quiz7TsBasicsId,
                    Title = "TypeScript: Type System & Generics Fundamentals",
                    Description = "Assess static typing concepts, type inference, interfaces, type aliases, union types, and generic functions in TypeScript.",
                    Duration = 20,
                    PassedScore = 70.0,
                    IsActive = true,
                    CreateAt = SeedTimestamp,
                    UpdateAt = SeedTimestamp
                },
                [
                    CreateQuestion(
                        19,
                        "What is the key difference between the 'unknown' and 'any' types in TypeScript?",
                        QuestionType.SingleChoice,
                        QuestionLevel.Medium,
                        [
                            ("'unknown' requires type narrowing or checking before performing operations, whereas 'any' disables type checking entirely", true),
                            ("'any' is type-safe at compile-time, while 'unknown' is not", false),
                            ("'unknown' can only represent boolean values", false),
                            ("There is no difference between them", false)
                        ]),
                    CreateQuestion(
                        20,
                        "Which TypeScript constructs can be used to define object shapes?",
                        QuestionType.MultipleChoice,
                        QuestionLevel.Medium,
                        [
                            ("interface User { name: string; }", true),
                            ("type User = { name: string; };", true),
                            ("class User { name!: string; }", true),
                            ("enum User { name }", false)
                        ]),
                    CreateQuestion(
                        21,
                        "In TypeScript, the array syntax 'T[]' and the generic syntax 'Array<T>' are completely equivalent in type meaning.",
                        QuestionType.TrueFalse,
                        QuestionLevel.Easy,
                        [
                            ("True", true),
                            ("False", false)
                        ])
                ]
            ),

            // 8. TypeScript Advanced Types
            new QuizSeedData(
                new Quiz
                {
                    Id = Quiz8TsAdvancedId,
                    Title = "TypeScript: Advanced Types & Utility Types",
                    Description = "Master advanced TypeScript features including conditional types, mapped types, index signatures, and built-in utility types.",
                    Duration = 25,
                    PassedScore = 75.0,
                    IsActive = true,
                    CreateAt = SeedTimestamp,
                    UpdateAt = SeedTimestamp
                },
                [
                    CreateQuestion(
                        22,
                        "Which utility type in TypeScript constructs a type with all properties of T set to optional?",
                        QuestionType.SingleChoice,
                        QuestionLevel.Easy,
                        [
                            ("Partial<T>", true),
                            ("Required<T>", false),
                            ("Readonly<T>", false),
                            ("Record<K, T>", false)
                        ]),
                    CreateQuestion(
                        23,
                        "Which utility type constructs a type by picking all properties from T and then removing keys specified in K?",
                        QuestionType.SingleChoice,
                        QuestionLevel.Medium,
                        [
                            ("Omit<T, K>", true),
                            ("Pick<T, K>", false),
                            ("Exclude<T, U>", false),
                            ("Extract<T, U>", false)
                        ]),
                    CreateQuestion(
                        24,
                        "The 'keyof T' operator in TypeScript produces a union type of all known property names (keys) of type T.",
                        QuestionType.TrueFalse,
                        QuestionLevel.Medium,
                        [
                            ("True", true),
                            ("False", false)
                        ])
                ]
            ),

            // 9. ASP.NET Core Web API
            new QuizSeedData(
                new Quiz
                {
                    Id = Quiz9ApiRestId,
                    Title = "ASP.NET Core: Web API & RESTful Architecture",
                    Description = "Test your understanding of REST API design, HTTP status codes, routing conventions, controller actions, and pipeline flow in ASP.NET Core.",
                    Duration = 20,
                    PassedScore = 70.0,
                    IsActive = true,
                    CreateAt = SeedTimestamp,
                    UpdateAt = SeedTimestamp
                },
                [
                    CreateQuestion(
                        25,
                        "According to RESTful standards, which HTTP status code is most appropriate when a new resource is created via a POST request?",
                        QuestionType.SingleChoice,
                        QuestionLevel.Easy,
                        [
                            ("201 Created", true),
                            ("200 OK", false),
                            ("204 No Content", false),
                            ("202 Accepted", false)
                        ]),
                    CreateQuestion(
                        26,
                        "Which of the following HTTP methods are considered idempotent by the HTTP specification?",
                        QuestionType.MultipleChoice,
                        QuestionLevel.Medium,
                        [
                            ("GET", true),
                            ("PUT", true),
                            ("DELETE", true),
                            ("POST", false)
                        ]),
                    CreateQuestion(
                        27,
                        "In ASP.NET Core, the order of middleware registration in Program.cs directly determines the order of request processing.",
                        QuestionType.TrueFalse,
                        QuestionLevel.Easy,
                        [
                            ("True", true),
                            ("False", false)
                        ])
                ]
            ),

            // 10. Web API Security & JWT
            new QuizSeedData(
                new Quiz
                {
                    Id = Quiz10ApiSecurityId,
                    Title = "Web API Security: JWT Authentication & Authorization",
                    Description = "Master token-based authentication, JWT structure, cryptographic signatures, ClaimsPrincipal validation, and role-based policies.",
                    Duration = 25,
                    PassedScore = 75.0,
                    IsActive = true,
                    CreateAt = SeedTimestamp,
                    UpdateAt = SeedTimestamp
                },
                [
                    CreateQuestion(
                        28,
                        "How many segments separated by periods (.) make up a standard JSON Web Token (JWT)?",
                        QuestionType.SingleChoice,
                        QuestionLevel.Easy,
                        [
                            ("3 (Header, Payload, Signature)", true),
                            ("2 (Header, Body)", false),
                            ("4 (Header, Claims, Signature, Hash)", false),
                            ("5 (Issuer, Subject, Claims, Hash, Signature)", false)
                        ]),
                    CreateQuestion(
                        29,
                        "Which standard HTTP header is used to pass a JWT bearer token to an API endpoint?",
                        QuestionType.SingleChoice,
                        QuestionLevel.Easy,
                        [
                            ("Authorization", true),
                            ("Authentication", false),
                            ("X-Token", false),
                            ("Bearer", false)
                        ]),
                    CreateQuestion(
                        30,
                        "The payload of a standard JWT is encrypted by default, preventing clients from inspecting claims without the secret key.",
                        QuestionType.TrueFalse,
                        QuestionLevel.Medium,
                        [
                            ("True", false),
                            ("False", true)
                        ])
                ]
            ),

            // 11. Entity Framework Core Modeling
            new QuizSeedData(
                new Quiz
                {
                    Id = Quiz11EfCoreModelingId,
                    Title = "Entity Framework Core: Data Modeling & Relationships",
                    Description = "Examine EF Core DbContext configuration, Fluent API entity mappings, composite keys, and shadow properties.",
                    Duration = 20,
                    PassedScore = 70.0,
                    IsActive = true,
                    CreateAt = SeedTimestamp,
                    UpdateAt = SeedTimestamp
                },
                [
                    CreateQuestion(
                        31,
                        "Which method on ModelBuilder in EF Core is the primary entry point for configuring entity mappings using the Fluent API?",
                        QuestionType.SingleChoice,
                        QuestionLevel.Easy,
                        [
                            ("Entity<T>()", true),
                            ("Configure<T>()", false),
                            ("Map<T>()", false),
                            ("Build<T>()", false)
                        ]),
                    CreateQuestion(
                        32,
                        "Which relationship types can be mapped natively using EF Core Fluent API?",
                        QuestionType.MultipleChoice,
                        QuestionLevel.Medium,
                        [
                            ("One-to-One", true),
                            ("One-to-Many", true),
                            ("Many-to-Many", true),
                            ("Polymorphic Table Inheritance", false)
                        ]),
                    CreateQuestion(
                        33,
                        "EF Core creates foreign key index conventions automatically for relational providers like SQL Server.",
                        QuestionType.TrueFalse,
                        QuestionLevel.Easy,
                        [
                            ("True", true),
                            ("False", false)
                        ])
                ]
            ),

            // 12. Entity Framework Core Optimization
            new QuizSeedData(
                new Quiz
                {
                    Id = Quiz12EfCoreOptimizationId,
                    Title = "Entity Framework Core: Query Optimization & Performance",
                    Description = "Test your knowledge of AsNoTracking, split queries, compiled queries, projection, and the N+1 query problem in EF Core.",
                    Duration = 25,
                    PassedScore = 75.0,
                    IsActive = true,
                    CreateAt = SeedTimestamp,
                    UpdateAt = SeedTimestamp
                },
                [
                    CreateQuestion(
                        34,
                        "Which EF Core method tells the change tracker not to track entities returned by a read-only query, improving performance?",
                        QuestionType.SingleChoice,
                        QuestionLevel.Easy,
                        [
                            ("AsNoTracking()", true),
                            ("AsReadOnly()", false),
                            ("WithoutTracking()", false),
                            ("DisableTracker()", false)
                        ]),
                    CreateQuestion(
                        35,
                        "What performance issue occurs when EF Core executes one query for a parent entity and subsequent separate queries for each related child?",
                        QuestionType.SingleChoice,
                        QuestionLevel.Medium,
                        [
                            ("N+1 Query Problem", true),
                            ("Cartesian Explosion", false),
                            ("Deadlock cascade", false),
                            ("Phantom read anomaly", false)
                        ]),
                    CreateQuestion(
                        36,
                        "Using projection with LINQ '.Select()' retrieves only the requested database columns rather than the entire entity table.",
                        QuestionType.TrueFalse,
                        QuestionLevel.Easy,
                        [
                            ("True", true),
                            ("False", false)
                        ])
                ]
            ),

            // 13. C# LINQ
            new QuizSeedData(
                new Quiz
                {
                    Id = Quiz13CsharpLinqId,
                    Title = "C# LINQ: Expressive Querying & Functional Patterns",
                    Description = "Assess advanced LINQ query syntax, method chaining, set operations, grouping, and projection techniques in C#.",
                    Duration = 20,
                    PassedScore = 70.0,
                    IsActive = true,
                    CreateAt = SeedTimestamp,
                    UpdateAt = SeedTimestamp
                },
                [
                    CreateQuestion(
                        37,
                        "Which LINQ method flattens collections of collections into a single sequence?",
                        QuestionType.SingleChoice,
                        QuestionLevel.Medium,
                        [
                            ("SelectMany()", true),
                            ("Select()", false),
                            ("FlatMap()", false),
                            ("Concat()", false)
                        ]),
                    CreateQuestion(
                        38,
                        "Which of the following LINQ methods immediately execute the query (greedy evaluation)?",
                        QuestionType.MultipleChoice,
                        QuestionLevel.Medium,
                        [
                            ("ToList()", true),
                            ("ToDictionary()", true),
                            ("Aggregate()", true),
                            ("Where()", false)
                        ]),
                    CreateQuestion(
                        39,
                        "The 'yield return' keyword enables custom iterators that produce elements on-demand with lazy evaluation.",
                        QuestionType.TrueFalse,
                        QuestionLevel.Easy,
                        [
                            ("True", true),
                            ("False", false)
                        ])
                ]
            ),

            // 14. ASP.NET Core Dependency Injection
            new QuizSeedData(
                new Quiz
                {
                    Id = Quiz14AspNetCoreDiId,
                    Title = "ASP.NET Core: Dependency Injection & Service Lifetimes",
                    Description = "Explore the built-in IoC container in ASP.NET Core, service registrations, and avoiding captive dependency pitfalls.",
                    Duration = 20,
                    PassedScore = 70.0,
                    IsActive = true,
                    CreateAt = SeedTimestamp,
                    UpdateAt = SeedTimestamp
                },
                [
                    CreateQuestion(
                        40,
                        "What service lifetime creates a single instance per HTTP request scope in ASP.NET Core?",
                        QuestionType.SingleChoice,
                        QuestionLevel.Easy,
                        [
                            ("Scoped", true),
                            ("Transient", false),
                            ("Singleton", false),
                            ("PerThread", false)
                        ]),
                    CreateQuestion(
                        41,
                        "What is a 'captive dependency' in ASP.NET Core DI?",
                        QuestionType.SingleChoice,
                        QuestionLevel.Hard,
                        [
                            ("When a Singleton service inadvertently depends on a Scoped service", true),
                            ("When two Scoped services create a circular dependency", false),
                            ("When a Transient service is injected into a Scoped service", false),
                            ("When a service fails to implement IDisposable", false)
                        ]),
                    CreateQuestion(
                        42,
                        "Transient services are newly created every time they are requested from the service container.",
                        QuestionType.TrueFalse,
                        QuestionLevel.Easy,
                        [
                            ("True", true),
                            ("False", false)
                        ])
                ]
            ),

            // 15. SQL Server Transactions & Locking
            new QuizSeedData(
                new Quiz
                {
                    Id = Quiz15SqlTransactionsId,
                    Title = "SQL Server: Transactions, Concurrency & Locking",
                    Description = "Understand transaction isolation levels, row versioning, dirty reads, non-repeatable reads, phantom reads, and deadlocks.",
                    Duration = 25,
                    PassedScore = 75.0,
                    IsActive = true,
                    CreateAt = SeedTimestamp,
                    UpdateAt = SeedTimestamp
                },
                [
                    CreateQuestion(
                        43,
                        "Which transaction isolation level in SQL Server prevents dirty reads, non-repeatable reads, and phantom reads?",
                        QuestionType.SingleChoice,
                        QuestionLevel.Medium,
                        [
                            ("SERIALIZABLE", true),
                            ("READ COMMITTED", false),
                            ("READ UNCOMMITTED", false),
                            ("REPEATABLE READ", false)
                        ]),
                    CreateQuestion(
                        44,
                        "Which concurrency anomalies are possible under the default READ COMMITTED isolation level?",
                        QuestionType.MultipleChoice,
                        QuestionLevel.Hard,
                        [
                            ("Non-repeatable reads", true),
                            ("Phantom reads", true),
                            ("Dirty reads", false),
                            ("Lost updates under strict lock", false)
                        ]),
                    CreateQuestion(
                        45,
                        "Snapshot Isolation in SQL Server uses row versioning in tempdb to provide point-in-time consistency without locking read rows.",
                        QuestionType.TrueFalse,
                        QuestionLevel.Medium,
                        [
                            ("True", true),
                            ("False", false)
                        ])
                ]
            ),

            // 16. Angular Change Detection
            new QuizSeedData(
                new Quiz
                {
                    Id = Quiz16AngularChangeDetectionId,
                    Title = "Angular: Change Detection & Performance Tuning",
                    Description = "Evaluate Angular change detection mechanisms, Zone.js, Zoneless Angular, and ChangeDetectionStrategy.OnPush.",
                    Duration = 20,
                    PassedScore = 70.0,
                    IsActive = true,
                    CreateAt = SeedTimestamp,
                    UpdateAt = SeedTimestamp
                },
                [
                    CreateQuestion(
                        46,
                        "When using 'ChangeDetectionStrategy.OnPush', when does Angular automatically check the component for updates?",
                        QuestionType.SingleChoice,
                        QuestionLevel.Medium,
                        [
                            ("When an input reference changes or an event originates inside the component", true),
                            ("On every browser timer tick regardless of inputs", false),
                            ("Only when manually calling ApplicationRef.tick()", false),
                            ("Every 100 milliseconds via background polling", false)
                        ]),
                    CreateQuestion(
                        47,
                        "Which techniques help optimize rendering performance in large Angular applications?",
                        QuestionType.MultipleChoice,
                        QuestionLevel.Medium,
                        [
                            ("Using @track in loop templates", true),
                            ("Using ChangeDetectionStrategy.OnPush", true),
                            ("Lazy loading routes", true),
                            ("Disabling Ahead-Of-Time (AOT) compilation", false)
                        ]),
                    CreateQuestion(
                        48,
                        "Angular 18+ introduced official support for experimental Zoneless change detection powered by Signals.",
                        QuestionType.TrueFalse,
                        QuestionLevel.Easy,
                        [
                            ("True", true),
                            ("False", false)
                        ])
                ]
            ),

            // 17. RxJS Operators
            new QuizSeedData(
                new Quiz
                {
                    Id = Quiz17RxjsOperatorsId,
                    Title = "RxJS: Observables, Subjects & Reactive Pipelines",
                    Description = "Test reactive programming concepts with cold/hot observables, BehaviorSubject, ReplaySubject, and combination operators.",
                    Duration = 25,
                    PassedScore = 75.0,
                    IsActive = true,
                    CreateAt = SeedTimestamp,
                    UpdateAt = SeedTimestamp
                },
                [
                    CreateQuestion(
                        49,
                        "Which RxJS Subject variant stores and immediately emits its latest current value to any new subscriber?",
                        QuestionType.SingleChoice,
                        QuestionLevel.Easy,
                        [
                            ("BehaviorSubject", true),
                            ("Subject", false),
                            ("AsyncSubject", false),
                            ("ReplaySubject(0)", false)
                        ]),
                    CreateQuestion(
                        50,
                        "Which RxJS combination operators can be used to join values from multiple observables?",
                        QuestionType.MultipleChoice,
                        QuestionLevel.Medium,
                        [
                            ("combineLatest", true),
                            ("forkJoin", true),
                            ("zip", true),
                            ("tap", false)
                        ]),
                    CreateQuestion(
                        51,
                        "The 'takeUntilDestroyed' operator in Angular automatically un-subscribes observables when the component context is destroyed.",
                        QuestionType.TrueFalse,
                        QuestionLevel.Easy,
                        [
                            ("True", true),
                            ("False", false)
                        ])
                ]
            ),

            // 18. Git Version Control
            new QuizSeedData(
                new Quiz
                {
                    Id = Quiz18GitVersionControlId,
                    Title = "Git Version Control: Branching, Merging & History",
                    Description = "Understand distributed version control, fast-forward merges, three-way merges, interactive rebase, and cherry-picking.",
                    Duration = 20,
                    PassedScore = 70.0,
                    IsActive = true,
                    CreateAt = SeedTimestamp,
                    UpdateAt = SeedTimestamp
                },
                [
                    CreateQuestion(
                        52,
                        "Which Git command is used to apply specific commits from one branch onto the current branch without merging the whole branch?",
                        QuestionType.SingleChoice,
                        QuestionLevel.Easy,
                        [
                            ("git cherry-pick", true),
                            ("git rebase", false),
                            ("git stash pop", false),
                            ("git merge --squash", false)
                        ]),
                    CreateQuestion(
                        53,
                        "Which of the following operations rewrite commit history in Git?",
                        QuestionType.MultipleChoice,
                        QuestionLevel.Medium,
                        [
                            ("git commit --amend", true),
                            ("git rebase -i", true),
                            ("git merge --no-ff", false),
                            ("git checkout -b", false)
                        ]),
                    CreateQuestion(
                        54,
                        "A fast-forward merge moves the branch pointer forward without creating a new merge commit.",
                        QuestionType.TrueFalse,
                        QuestionLevel.Easy,
                        [
                            ("True", true),
                            ("False", false)
                        ])
                ]
            ),

            // 19. Clean Code & SOLID Principles
            new QuizSeedData(
                new Quiz
                {
                    Id = Quiz19CleanCodeSolidId,
                    Title = "Software Design: SOLID Principles & Clean Architecture",
                    Description = "Assess your mastery of Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, and Dependency Inversion.",
                    Duration = 20,
                    PassedScore = 70.0,
                    IsActive = true,
                    CreateAt = SeedTimestamp,
                    UpdateAt = SeedTimestamp
                },
                [
                    CreateQuestion(
                        55,
                        "Which SOLID principle states that high-level modules should not depend on low-level modules, but both should depend on abstractions?",
                        QuestionType.SingleChoice,
                        QuestionLevel.Medium,
                        [
                            ("Dependency Inversion Principle", true),
                            ("Interface Segregation Principle", false),
                            ("Single Responsibility Principle", false),
                            ("Open/Closed Principle", false)
                        ]),
                    CreateQuestion(
                        56,
                        "What are primary architectural characteristics of Clean Architecture?",
                        QuestionType.MultipleChoice,
                        QuestionLevel.Medium,
                        [
                            ("Independent of frameworks", true),
                            ("Business logic decoupled from UI and database", true),
                            ("Testable core domain rules", true),
                            ("Tight coupling between domain entities and ORM tables", false)
                        ]),
                    CreateQuestion(
                        57,
                        "The Open/Closed Principle encourages classes to be open for extension but closed for modification.",
                        QuestionType.TrueFalse,
                        QuestionLevel.Easy,
                        [
                            ("True", true),
                            ("False", false)
                        ])
                ]
            ),

            // 20. Design Patterns GoF
            new QuizSeedData(
                new Quiz
                {
                    Id = Quiz20DesignPatternsGofId,
                    Title = "Design Patterns: Gang of Four Creational & Structural",
                    Description = "Test classical software design patterns: Factory Method, Singleton, Builder, Adapter, Decorator, and Facade.",
                    Duration = 25,
                    PassedScore = 75.0,
                    IsActive = true,
                    CreateAt = SeedTimestamp,
                    UpdateAt = SeedTimestamp
                },
                [
                    CreateQuestion(
                        58,
                        "Which structural design pattern converts the interface of a class into another interface that clients expect?",
                        QuestionType.SingleChoice,
                        QuestionLevel.Easy,
                        [
                            ("Adapter", true),
                            ("Decorator", false),
                            ("Facade", false),
                            ("Composite", false)
                        ]),
                    CreateQuestion(
                        59,
                        "Which creational design pattern separates the construction of a complex object from its representation?",
                        QuestionType.SingleChoice,
                        QuestionLevel.Medium,
                        [
                            ("Builder", true),
                            ("Factory Method", false),
                            ("Prototype", false),
                            ("Singleton", false)
                        ]),
                    CreateQuestion(
                        60,
                        "The Decorator pattern dynamically adds responsibilities to objects without modifying the underlying class or using inheritance.",
                        QuestionType.TrueFalse,
                        QuestionLevel.Medium,
                        [
                            ("True", true),
                            ("False", false)
                        ])
                ]
            ),

            // 21. Web Security OWASP
            new QuizSeedData(
                new Quiz
                {
                    Id = Quiz21WebSecurityOwaspId,
                    Title = "Web Application Security: OWASP Top 10 Mitigation",
                    Description = "Explore critical security flaws: SQL Injection, Cross-Site Scripting (XSS), CSRF, broken access control, and security headers.",
                    Duration = 25,
                    PassedScore = 75.0,
                    IsActive = true,
                    CreateAt = SeedTimestamp,
                    UpdateAt = SeedTimestamp
                },
                [
                    CreateQuestion(
                        61,
                        "What is the primary defense against SQL Injection vulnerabilities in database-driven applications?",
                        QuestionType.SingleChoice,
                        QuestionLevel.Easy,
                        [
                            ("Using parameterized queries or object-relational mappers", true),
                            ("Storing passwords with MD5 hashing", false),
                            ("Escaping HTML characters on the client", false),
                            ("Disabling HTTP POST requests", false)
                        ]),
                    CreateQuestion(
                        62,
                        "Which HTTP headers help mitigate common browser-based vulnerabilities?",
                        QuestionType.MultipleChoice,
                        QuestionLevel.Medium,
                        [
                            ("Content-Security-Policy", true),
                            ("X-Frame-Options", true),
                            ("Strict-Transport-Security", true),
                            ("Accept-Encoding", false)
                        ]),
                    CreateQuestion(
                        63,
                        "Cross-Site Request Forgery (CSRF) exploits the trust that a web application has in the user's authenticated browser session.",
                        QuestionType.TrueFalse,
                        QuestionLevel.Easy,
                        [
                            ("True", true),
                            ("False", false)
                        ])
                ]
            ),

            // 22. Docker Containers
            new QuizSeedData(
                new Quiz
                {
                    Id = Quiz22DockerContainersId,
                    Title = "Docker: Containers, Images & Multi-Stage Builds",
                    Description = "Evaluate containerization principles, Dockerfile optimization, multi-stage compilation, and layer caching strategies.",
                    Duration = 20,
                    PassedScore = 70.0,
                    IsActive = true,
                    CreateAt = SeedTimestamp,
                    UpdateAt = SeedTimestamp
                },
                [
                    CreateQuestion(
                        64,
                        "What is the primary benefit of using multi-stage builds in a Dockerfile for production applications?",
                        QuestionType.SingleChoice,
                        QuestionLevel.Medium,
                        [
                            ("Minimizing the final image size by excluding build tools and source code", true),
                            ("Speeding up DNS resolution inside containers", false),
                            ("Enabling root privileges for build scripts", false),
                            ("Running multiple background containers in a single image", false)
                        ]),
                    CreateQuestion(
                        65,
                        "Which of the following Dockerfile instructions create a new image layer?",
                        QuestionType.MultipleChoice,
                        QuestionLevel.Medium,
                        [
                            ("RUN", true),
                            ("COPY", true),
                            ("ADD", true),
                            ("EXPOSE", false)
                        ]),
                    CreateQuestion(
                        66,
                        "Containers share the host operating system kernel rather than virtualizing full guest operating system hardware.",
                        QuestionType.TrueFalse,
                        QuestionLevel.Easy,
                        [
                            ("True", true),
                            ("False", false)
                        ])
                ]
            ),

            // 23. Unit Testing & TDD
            new QuizSeedData(
                new Quiz
                {
                    Id = Quiz23UnitTestingTddId,
                    Title = "Unit Testing: Test-Driven Development & Best Practices",
                    Description = "Examine TDD red-green-refactor loop, test doubles (mocks, stubs, fakes), assertions, and test isolation principles.",
                    Duration = 20,
                    PassedScore = 70.0,
                    IsActive = true,
                    CreateAt = SeedTimestamp,
                    UpdateAt = SeedTimestamp
                },
                [
                    CreateQuestion(
                        67,
                        "In the Test-Driven Development (TDD) cycle, what is the mandatory first step before writing production code?",
                        QuestionType.SingleChoice,
                        QuestionLevel.Easy,
                        [
                            ("Write a failing unit test that specifies the desired behavior", true),
                            ("Write the complete implementation class", false),
                            ("Write end-to-end integration tests", false),
                            ("Refactor the existing codebase", false)
                        ]),
                    CreateQuestion(
                        68,
                        "Which properties are essential for high-quality unit tests?",
                        QuestionType.MultipleChoice,
                        QuestionLevel.Medium,
                        [
                            ("Fast execution speed", true),
                            ("Deterministic and repeatable results", true),
                            ("Isolated with no shared state between tests", true),
                            ("Direct dependency on external production databases", false)
                        ]),
                    CreateQuestion(
                        69,
                        "A Mock object verifies interactions and method calls, whereas a Stub provides canned responses without behavioral verification.",
                        QuestionType.TrueFalse,
                        QuestionLevel.Medium,
                        [
                            ("True", true),
                            ("False", false)
                        ])
                ]
            ),

            // 24. Microservices Patterns
            new QuizSeedData(
                new Quiz
                {
                    Id = Quiz24MicroservicesPatternsId,
                    Title = "Microservices: Distributed Architecture & Patterns",
                    Description = "Test concepts in distributed systems: API Gateway, Saga pattern, Circuit Breaker, Outbox pattern, and eventual consistency.",
                    Duration = 25,
                    PassedScore = 75.0,
                    IsActive = true,
                    CreateAt = SeedTimestamp,
                    UpdateAt = SeedTimestamp
                },
                [
                    CreateQuestion(
                        70,
                        "Which pattern manages distributed transactions across multiple microservices via a series of local transactions and compensating actions?",
                        QuestionType.SingleChoice,
                        QuestionLevel.Hard,
                        [
                            ("Saga Pattern", true),
                            ("Circuit Breaker Pattern", false),
                            ("CQRS Pattern", false),
                            ("Strangler Fig Pattern", false)
                        ]),
                    CreateQuestion(
                        71,
                        "Which pattern prevents a service from repeatedly trying to execute an operation that is likely to fail, giving downstream systems time to recover?",
                        QuestionType.SingleChoice,
                        QuestionLevel.Medium,
                        [
                            ("Circuit Breaker", true),
                            ("Bulkhead", false),
                            ("Ambassador", false),
                            ("Sidecar", false)
                        ]),
                    CreateQuestion(
                        72,
                        "The Transactional Outbox pattern guarantees reliable messaging between microservices by atomically saving events to the local database before publishing.",
                        QuestionType.TrueFalse,
                        QuestionLevel.Medium,
                        [
                            ("True", true),
                            ("False", false)
                        ])
                ]
            ),

            // 25. RESTful API Conventions
            new QuizSeedData(
                new Quiz
                {
                    Id = Quiz25RestfulApiConventionsId,
                    Title = "API Engineering: REST Conventions & HTTP Protocols",
                    Description = "Understand URI naming conventions, query pagination, content negotiation, HTTP/2, and status codes.",
                    Duration = 20,
                    PassedScore = 70.0,
                    IsActive = true,
                    CreateAt = SeedTimestamp,
                    UpdateAt = SeedTimestamp
                },
                [
                    CreateQuestion(
                        73,
                        "Which HTTP status code should an API return when a client makes a valid request to delete an existing resource and no response body is returned?",
                        QuestionType.SingleChoice,
                        QuestionLevel.Easy,
                        [
                            ("204 No Content", true),
                            ("200 OK", false),
                            ("202 Accepted", false),
                            ("201 Created", false)
                        ]),
                    CreateQuestion(
                        74,
                        "Which URI structures follow idiomatic RESTful naming conventions for resource hierarchies?",
                        QuestionType.MultipleChoice,
                        QuestionLevel.Medium,
                        [
                            ("/api/quizzes/123/questions", true),
                            ("/api/users/456/attempts", true),
                            ("/api/getQuizzesList", false),
                            ("/api/submitQuizAnswersNow", false)
                        ]),
                    CreateQuestion(
                        75,
                        "In HTTP/2, multiplexing allows multiple concurrent requests and responses over a single TCP connection, eliminating head-of-line blocking at the application layer.",
                        QuestionType.TrueFalse,
                        QuestionLevel.Easy,
                        [
                            ("True", true),
                            ("False", false)
                        ])
                ]
            ),

            // 26. CI/CD Pipelines
            new QuizSeedData(
                new Quiz
                {
                    Id = Quiz26CiCdPipelinesId,
                    Title = "DevOps: Continuous Integration & Automated Pipelines",
                    Description = "Examine modern CI/CD practices: build runners, artifact repositories, automated gates, and deployment strategies.",
                    Duration = 20,
                    PassedScore = 70.0,
                    IsActive = true,
                    CreateAt = SeedTimestamp,
                    UpdateAt = SeedTimestamp
                },
                [
                    CreateQuestion(
                        76,
                        "What is the primary objective of Continuous Integration (CI) in professional software engineering?",
                        QuestionType.SingleChoice,
                        QuestionLevel.Easy,
                        [
                            ("Frequently integrating and validating code changes through automated builds and tests", true),
                            ("Deploying directly to production on every keystroke", false),
                            ("Eliminating the need for automated tests", false),
                            ("Storing build artifacts indefinitely", false)
                        ]),
                    CreateQuestion(
                        77,
                        "Which deployment strategies minimize application downtime during production releases?",
                        QuestionType.MultipleChoice,
                        QuestionLevel.Medium,
                        [
                            ("Blue-Green Deployment", true),
                            ("Canary Release", true),
                            ("Rolling Update", true),
                            ("Big Bang Replacement", false)
                        ]),
                    CreateQuestion(
                        78,
                        "In a Canary deployment, new application features are initially exposed to a small subset of users before rolling out to the entire fleet.",
                        QuestionType.TrueFalse,
                        QuestionLevel.Easy,
                        [
                            ("True", true),
                            ("False", false)
                        ])
                ]
            ),

            // 27. Web Performance
            new QuizSeedData(
                new Quiz
                {
                    Id = Quiz27WebPerformanceId,
                    Title = "Web Performance: Optimization & Browser Caching",
                    Description = "Test optimization techniques: Core Web Vitals, code splitting, lazy loading, compression, and Cache-Control headers.",
                    Duration = 20,
                    PassedScore = 70.0,
                    IsActive = true,
                    CreateAt = SeedTimestamp,
                    UpdateAt = SeedTimestamp
                },
                [
                    CreateQuestion(
                        79,
                        "Which Core Web Vital metric measures perceived loading speed by recording when the main content of a page is likely loaded?",
                        QuestionType.SingleChoice,
                        QuestionLevel.Medium,
                        [
                            ("Largest Contentful Paint (LCP)", true),
                            ("First Input Delay (FID)", false),
                            ("Cumulative Layout Shift (CLS)", false),
                            ("Interaction to Next Paint (INP)", false)
                        ]),
                    CreateQuestion(
                        80,
                        "Which techniques reduce initial JavaScript bundle download sizes in single-page applications?",
                        QuestionType.MultipleChoice,
                        QuestionLevel.Medium,
                        [
                            ("Route-level code splitting", true),
                            ("Tree shaking of unused exports", true),
                            ("Modern asset compression like Brotli", true),
                            ("Embedding large uncompressed images as base64 in bundles", false)
                        ]),
                    CreateQuestion(
                        81,
                        "The Cache-Control header 'no-cache' means the browser must revalidate the cached response with the origin server before using it.",
                        QuestionType.TrueFalse,
                        QuestionLevel.Medium,
                        [
                            ("True", true),
                            ("False", false)
                        ])
                ]
            ),

            // 28. ASP.NET Core Middleware
            new QuizSeedData(
                new Quiz
                {
                    Id = Quiz28AspNetCoreMiddlewareId,
                    Title = "ASP.NET Core: Pipeline Architecture & Custom Middleware",
                    Description = "Understand HttpContext processing, terminal middleware, short-circuiting, and centralized exception handling in ASP.NET Core.",
                    Duration = 20,
                    PassedScore = 70.0,
                    IsActive = true,
                    CreateAt = SeedTimestamp,
                    UpdateAt = SeedTimestamp
                },
                [
                    CreateQuestion(
                        82,
                        "How does a middleware component pass control to the subsequent middleware component in the ASP.NET Core pipeline?",
                        QuestionType.SingleChoice,
                        QuestionLevel.Easy,
                        [
                            ("By invoking the 'next(context)' RequestDelegate", true),
                            ("By calling HttpContext.Redirect()", false),
                            ("By throwing an unhandled exception", false),
                            ("By returning Task.Delay()", false)
                        ]),
                    CreateQuestion(
                        83,
                        "Which scenarios are appropriate for custom middleware in ASP.NET Core?",
                        QuestionType.MultipleChoice,
                        QuestionLevel.Medium,
                        [
                            ("Global exception handling and ProblemDetails transformation", true),
                            ("Request logging and latency metrics", true),
                            ("Security headers injection", true),
                            ("Complex business logic calculations", false)
                        ]),
                    CreateQuestion(
                        84,
                        "If a middleware component generates a response and does not invoke the 'next' delegate, the pipeline short-circuits and begins executing preceding middleware in reverse.",
                        QuestionType.TrueFalse,
                        QuestionLevel.Easy,
                        [
                            ("True", true),
                            ("False", false)
                        ])
                ]
            ),

            // 29. Angular Reactive Forms
            new QuizSeedData(
                new Quiz
                {
                    Id = Quiz29AngularFormsValidationId,
                    Title = "Angular: Reactive Forms & Advanced Validation",
                    Description = "Deep dive into FormBuilder, FormArray, custom sync and async validators, and reactive form state management.",
                    Duration = 20,
                    PassedScore = 70.0,
                    IsActive = true,
                    CreateAt = SeedTimestamp,
                    UpdateAt = SeedTimestamp
                },
                [
                    CreateQuestion(
                        85,
                        "In Angular Reactive Forms, which class is specifically designed to manage a dynamically sized, ordered list of controls?",
                        QuestionType.SingleChoice,
                        QuestionLevel.Easy,
                        [
                            ("FormArray", true),
                            ("FormGroup", false),
                            ("FormControl", false),
                            ("FormRecord", false)
                        ]),
                    CreateQuestion(
                        86,
                        "What can an Angular custom synchronous validator function return?",
                        QuestionType.MultipleChoice,
                        QuestionLevel.Medium,
                        [
                            ("ValidationErrors object when invalid", true),
                            ("null when valid", true),
                            ("boolean true when valid", false),
                            ("Promise<ValidationErrors> for sync validation", false)
                        ]),
                    CreateQuestion(
                        87,
                        "Async validators in Angular run only after all synchronous validators have passed successfully.",
                        QuestionType.TrueFalse,
                        QuestionLevel.Easy,
                        [
                            ("True", true),
                            ("False", false)
                        ])
                ]
            ),

            // 30. TypeScript Generics & Constraints
            new QuizSeedData(
                new Quiz
                {
                    Id = Quiz30TsGenericsConstraintsId,
                    Title = "TypeScript: Generics & Type Constraint Engineering",
                    Description = "Master generic constraints with 'extends', indexed access types, infer keyword, and mapped object transforms.",
                    Duration = 25,
                    PassedScore = 75.0,
                    IsActive = true,
                    CreateAt = SeedTimestamp,
                    UpdateAt = SeedTimestamp
                },
                [
                    CreateQuestion(
                        88,
                        "Which TypeScript syntax ensures that a generic type parameter T must be an object with an 'id' property of type string?",
                        QuestionType.SingleChoice,
                        QuestionLevel.Easy,
                        [
                            ("<T extends { id: string }>", true),
                            ("<T implements { id: string }>", false),
                            ("<T : { id: string }>", false),
                            ("<T of { id: string }>", false)
                        ]),
                    CreateQuestion(
                        89,
                        "Which statements about generic functions in TypeScript are true?",
                        QuestionType.MultipleChoice,
                        QuestionLevel.Medium,
                        [
                            ("They promote reusable, type-safe logic across different data types", true),
                            ("Type arguments can often be inferred automatically by the compiler", true),
                            ("They can accept multiple type parameters like <T, U, R>", true),
                            ("Generic functions always execute slower at runtime in JavaScript", false)
                        ]),
                    CreateQuestion(
                        90,
                        "In TypeScript, the 'infer' keyword can only be used within the conditional type 'extends' clause.",
                        QuestionType.TrueFalse,
                        QuestionLevel.Medium,
                        [
                            ("True", true),
                            ("False", false)
                        ])
                ]
            )
        ];
    }

    private static QuestionSeedData CreateQuestion(
        int questionNumber,
        string content,
        QuestionType type,
        QuestionLevel level,
        (string text, bool isCorrect)[] options)
    {
        var questionId = Guid.Parse($"20000000-0000-0000-0000-{questionNumber:D12}");
        var question = new Question
        {
            Id = questionId,
            Content = content,
            QuestionType = type,
            Level = level,
            IsActive = true
        };

        var answers = options.Select((opt, idx) => new Answer
        {
            Id = Guid.Parse($"30000000-0000-0000-{questionNumber:D4}-{(idx + 1):D12}"),
            QuestionId = questionId,
            Text = opt.text,
            IsCorrect = opt.isCorrect,
            IsActive = true
        }).ToArray();

        return new QuestionSeedData(question, answers);
    }
}
