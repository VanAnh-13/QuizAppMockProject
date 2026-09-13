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

    // 10 Sample Quizzes
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

    public static readonly Guid[] SampleQuizIds =
    [
        Quiz1CsharpOopId, Quiz2CsharpAdvId, Quiz3SqlTsqlId, Quiz4SqlDesignId, Quiz5AngularCompId,
        Quiz6AngularRouteId, Quiz7TsBasicsId, Quiz8TsAdvancedId, Quiz9ApiRestId, Quiz10ApiSecurityId
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
                Description = "Quản trị viên hệ thống"
            };
            db.Roles.Add(adminRole);
        }

        var userRole = await db.Roles
            .FirstOrDefaultAsync(role => role.RoleName == "User", cancellationToken);
        if (userRole is null)
        {
            userRole = new Role
            {
                Id = RoleUserId,
                RoleName = "User",
                Description = "Người dùng tham gia bài thi"
            };
            db.Roles.Add(userRole);
        }

        var demoUser = await db.Users
            .FirstOrDefaultAsync(user => user.Username == "demo_user", cancellationToken)
            ?? await db.Users
                .FirstOrDefaultAsync(user => user.Email == "demo@quizapp.local", cancellationToken);
        if (demoUser is null)
        {
            var hasher = new PasswordHasher<object>();
            var passwordHash = hasher.HashPassword(new object(), "Password123!");

            demoUser = new User
            {
                Id = DemoUserId,
                Username = "demo_user",
                Email = "demo@quizapp.local",
                Password = passwordHash,
                FullName = "Học viên Demo",
                Status = UserStatus.Active,
                SecurityStamp = Guid.Parse("c0000000-0000-0000-0000-000000000001"),
                CreateAt = SeedTimestamp,
                UpdateAt = SeedTimestamp
            };
            db.Users.Add(demoUser);
        }

        var hasUserRoleAssignment = await db.UserRoles.AnyAsync(
            assignment => assignment.UserId == demoUser.Id && assignment.RoleId == userRole.Id,
            cancellationToken);
        if (!hasUserRoleAssignment)
        {
            db.UserRoles.Add(new UserRole
            {
                UserId = demoUser.Id,
                RoleId = userRole.Id
            });
        }
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
                            // Update existing answer
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

            // Clean up any stale dummy question associations on this sample quiz
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
                    Title = "C# Cơ bản & Lập trình hướng đối tượng (OOP)",
                    Description = "Bài kiểm tra đánh giá toàn diện năng lực lập trình hướng đối tượng trong hệ sinh thái C# & .NET. Thử thách tập trung vào các nguyên lý cốt lõi OOP, quản lý bộ nhớ CLR, các cấu trúc cú pháp hiện đại và ứng dụng thực tiễn trong xây dựng ứng dụng doanh nghiệp.",
                    Duration = 20,
                    PassedScore = 70.0,
                    IsActive = true,
                    CreateAt = SeedTimestamp,
                    UpdateAt = SeedTimestamp
                },
                [
                    CreateQuestion(
                        1,
                        "Trong C#, từ khóa nào được sử dụng để ngăn chặn một lớp không cho phép các lớp khác kế thừa?",
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
                        "Những đặc điểm nào sau đây là nguyên lý cốt lõi của Lập trình hướng đối tượng (OOP)?",
                        QuestionType.MultipleChoice,
                        QuestionLevel.Medium,
                        [
                            ("Đóng gói (Encapsulation)", true),
                            ("Kế thừa (Inheritance)", true),
                            ("Đa hình (Polymorphism)", true),
                            ("Tự động biên dịch AOT", false)
                        ]),
                    CreateQuestion(
                        3,
                        "Trong C#, một class có thể kế thừa trực tiếp từ bao nhiêu class cha (đơn kế thừa)?",
                        QuestionType.SingleChoice,
                        QuestionLevel.Easy,
                        [
                            ("1", true),
                            ("2", false),
                            ("Không giới hạn", false),
                            ("0", false)
                        ]),
                    CreateQuestion(
                        4,
                        "Trong C#, một class có thể triển khai (implement) nhiều Interface cùng một lúc.",
                        QuestionType.TrueFalse,
                        QuestionLevel.Easy,
                        [
                            ("Đúng", true),
                            ("Sai", false)
                        ])
                ]
            ),

            // 2. C# Advanced
            new QuizSeedData(
                new Quiz
                {
                    Id = Quiz2CsharpAdvId,
                    Title = "C# Nâng cao: LINQ, Asynchronous & CLR",
                    Description = "Thử thách kiến thức nâng cao về lập trình bất đồng bộ với async/await, tối ưu hóa truy vấn dữ liệu LINQ, bộ thu gom rác Garbage Collector và quản lý bộ nhớ trong .NET CLR.",
                    Duration = 25,
                    PassedScore = 75.0,
                    IsActive = true,
                    CreateAt = SeedTimestamp,
                    UpdateAt = SeedTimestamp
                },
                [
                    CreateQuestion(
                        5,
                        "Từ khóa 'await' trong C# có tác dụng chính nào sau đây?",
                        QuestionType.SingleChoice,
                        QuestionLevel.Medium,
                        [
                            ("Tạm dừng phương thức async mà không khóa thread hiện tại cho đến khi Task hoàn thành", true),
                            ("Chặn luồng hiện tại hoàn toàn giống như Thread.Sleep()", false),
                            ("Tạo một Thread nền mới từ hệ điều hành", false),
                            ("Hủy bỏ Task nếu thời gian chờ vượt quá 5 giây", false)
                        ]),
                    CreateQuestion(
                        6,
                        "Những phương thức nào sau đây trong LINQ sử dụng cơ chế Deferred Execution (thực thi trì hoãn)?",
                        QuestionType.MultipleChoice,
                        QuestionLevel.Hard,
                        [
                            ("Where(...)", true),
                            ("Select(...)", true),
                            ("ToList()", false),
                            ("Count()", false)
                        ]),
                    CreateQuestion(
                        7,
                        "Trong C#, struct là kiểu tham trị (Value Type) và các biến cục bộ kiểu struct thường được cấp phát trên Stack.",
                        QuestionType.TrueFalse,
                        QuestionLevel.Medium,
                        [
                            ("Đúng", true),
                            ("Sai", false)
                        ])
                ]
            ),

            // 3. SQL Server T-SQL
            new QuizSeedData(
                new Quiz
                {
                    Id = Quiz3SqlTsqlId,
                    Title = "SQL Server: Truy vấn T-SQL & Xử lý dữ liệu",
                    Description = "Đánh giá kỹ năng viết câu lệnh truy vấn T-SQL từ cơ bản đến nâng cao trong SQL Server, bao gồm JOIN, GROUP BY, HAVING, subquery và các hàm tổng hợp dữ liệu.",
                    Duration = 20,
                    PassedScore = 70.0,
                    IsActive = true,
                    CreateAt = SeedTimestamp,
                    UpdateAt = SeedTimestamp
                },
                [
                    CreateQuestion(
                        8,
                        "Trong SQL Server, mệnh đề nào được sử dụng để lọc dữ liệu trên các nhóm sau khi đã thực hiện GROUP BY?",
                        QuestionType.SingleChoice,
                        QuestionLevel.Easy,
                        [
                            ("WHERE", false),
                            ("HAVING", true),
                            ("ORDER BY", false),
                            ("FILTER", false)
                        ]),
                    CreateQuestion(
                        9,
                        "Điểm khác biệt chính giữa toán tử UNION và UNION ALL trong T-SQL là gì?",
                        QuestionType.SingleChoice,
                        QuestionLevel.Medium,
                        [
                            ("UNION loại bỏ các bản ghi trùng lặp, còn UNION ALL giữ lại toàn bộ bản ghi trùng lặp", true),
                            ("UNION ALL loại bỏ bản ghi trùng lặp, còn UNION giữ nguyên", false),
                            ("UNION chỉ dùng được trên kiểu dữ liệu số nguyên", false),
                            ("UNION ALL tự động sắp xếp kết quả theo thứ tự tăng dần", false)
                        ]),
                    CreateQuestion(
                        10,
                        "Những hàm nào sau đây là hàm tổng hợp (Aggregate Function) hợp lệ trong T-SQL?",
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
                    Title = "SQL Server: Thiết kế CSDL & Tối ưu hóa Index",
                    Description = "Chuyên đề về thiết kế cơ sở dữ liệu quan hệ, nguyên tắc chuẩn hóa dữ liệu, kiến trúc Clustered Index, Non-Clustered Index và tối ưu hóa hiệu năng truy vấn trong SQL Server.",
                    Duration = 25,
                    PassedScore = 70.0,
                    IsActive = true,
                    CreateAt = SeedTimestamp,
                    UpdateAt = SeedTimestamp
                },
                [
                    CreateQuestion(
                        11,
                        "Trong một bảng vật lý của SQL Server, bạn có thể tạo tối đa bao nhiêu Clustered Index?",
                        QuestionType.SingleChoice,
                        QuestionLevel.Easy,
                        [
                            ("1", true),
                            ("2", false),
                            ("249", false),
                            ("Không giới hạn", false)
                        ]),
                    CreateQuestion(
                        12,
                        "Các đặc tính nào sau đây tạo nên nguyên lý ACID trong hệ quản trị cơ sở dữ liệu quan hệ?",
                        QuestionType.MultipleChoice,
                        QuestionLevel.Hard,
                        [
                            ("Atomicity (Tính nguyên tử)", true),
                            ("Consistency (Tính nhất quán)", true),
                            ("Isolation (Tính cô lập)", true),
                            ("Durability (Tính bền vững)", true)
                        ]),
                    CreateQuestion(
                        13,
                        "Non-Clustered Index trong SQL Server làm thay đổi thứ tự sắp xếp vật lý của các dòng dữ liệu trên ổ đĩa.",
                        QuestionType.TrueFalse,
                        QuestionLevel.Medium,
                        [
                            ("Đúng", false),
                            ("Sai", true)
                        ])
                ]
            ),

            // 5. Angular Components & Signals
            new QuizSeedData(
                new Quiz
                {
                    Id = Quiz5AngularCompId,
                    Title = "Angular: Component, Signals & Dependency Injection",
                    Description = "Tìm hiểu các khái niệm nền tảng và tính năng mới nhất trong Angular: Reactive Signals, Standalone Components, cơ chế phát hiện thay đổi và Dependency Injection container.",
                    Duration = 20,
                    PassedScore = 70.0,
                    IsActive = true,
                    CreateAt = SeedTimestamp,
                    UpdateAt = SeedTimestamp
                },
                [
                    CreateQuestion(
                        14,
                        "Trong Angular hiện đại (từ v16+), hàm nào sau đây được sử dụng để khởi tạo một Signal có thể ghi giá trị (writable signal)?",
                        QuestionType.SingleChoice,
                        QuestionLevel.Easy,
                        [
                            ("signal()", true),
                            ("computed()", false),
                            ("effect()", false),
                            ("state()", false)
                        ]),
                    CreateQuestion(
                        15,
                        "Khi một Angular Service được trang trí với decorator '@Injectable({ providedIn: 'root' })', phạm vi vòng đời của nó là gì?",
                        QuestionType.SingleChoice,
                        QuestionLevel.Medium,
                        [
                            ("Singleton duy nhất trong toàn bộ ứng dụng", true),
                            ("Mỗi component inject service sẽ nhận một instance mới", false),
                            ("Mỗi route tạo một instance riêng biệt", false),
                            ("Chỉ tồn tại trong module khai báo", false)
                        ]),
                    CreateQuestion(
                        16,
                        "Những hàm nào sau đây thuộc hệ thống Angular Signals core reactivity?",
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
                    Title = "Angular: Routing, Forms & RxJS Observables",
                    Description = "Đánh giá năng lực phát triển ứng dụng Angular chuyên nghiệp với Router Guards, Reactive Forms, FormBuilder, quản lý luồng dữ liệu bất đồng bộ với RxJS operators.",
                    Duration = 25,
                    PassedScore = 70.0,
                    IsActive = true,
                    CreateAt = SeedTimestamp,
                    UpdateAt = SeedTimestamp
                },
                [
                    CreateQuestion(
                        17,
                        "Trong Angular Router, Guard nào được sử dụng phổ biến nhất để kiểm tra người dùng đã đăng nhập trước khi cho phép kích hoạt route?",
                        QuestionType.SingleChoice,
                        QuestionLevel.Medium,
                        [
                            ("canActivate", true),
                            ("canDeactivate", false),
                            ("resolve", false),
                            ("canMatchChildren", false)
                        ]),
                    CreateQuestion(
                        18,
                        "Toán tử RxJS nào sẽ hủy bỏ (cancel) inner observable đang chạy khi có giá trị mới được phát ra từ source observable?",
                        QuestionType.SingleChoice,
                        QuestionLevel.Medium,
                        [
                            ("switchMap", true),
                            ("mergeMap", false),
                            ("concatMap", false),
                            ("exhaustMap", false)
                        ]),
                    CreateQuestion(
                        19,
                        "Trong Angular Reactive Forms, FormGroup cho phép theo dõi giá trị và trạng thái validation của một tập hợp các FormControl con.",
                        QuestionType.TrueFalse,
                        QuestionLevel.Easy,
                        [
                            ("Đúng", true),
                            ("Sai", false)
                        ])
                ]
            ),

            // 7. TypeScript Basics & Generics
            new QuizSeedData(
                new Quiz
                {
                    Id = Quiz7TsBasicsId,
                    Title = "TypeScript: Hệ thống Type & Generics căn bản",
                    Description = "Kiểm tra kiến thức cốt lõi về TypeScript: Static Typing, Type Inference, Interfaces, Type Aliases, Union Types và Generic Functions giúp viết code an toàn, dễ bảo trì.",
                    Duration = 20,
                    PassedScore = 70.0,
                    IsActive = true,
                    CreateAt = SeedTimestamp,
                    UpdateAt = SeedTimestamp
                },
                [
                    CreateQuestion(
                        20,
                        "Sự khác biệt căn bản giữa kiểu 'unknown' và 'any' trong TypeScript là gì?",
                        QuestionType.SingleChoice,
                        QuestionLevel.Medium,
                        [
                            ("'unknown' yêu cầu kiểm tra kiểu (type check / narrowing) trước khi thực hiện thao tác, còn 'any' bỏ qua toàn bộ kiểm tra kiểu", true),
                            ("'any' an toàn hơn 'unknown' vì bắt lỗi lúc compile-time", false),
                            ("'unknown' chỉ có thể gán cho kiểu boolean", false),
                            ("Không có sự khác nhau về mặt kiểm tra kiểu", false)
                        ]),
                    CreateQuestion(
                        21,
                        "Những cú pháp nào sau đây trong TypeScript có thể được dùng để định nghĩa kiểu dữ liệu cho một đối tượng?",
                        QuestionType.MultipleChoice,
                        QuestionLevel.Medium,
                        [
                            ("interface User { name: string; }", true),
                            ("type User = { name: string; };", true),
                            ("class User { name!: string; }", true),
                            ("enum User { name }", false)
                        ]),
                    CreateQuestion(
                        22,
                        "Trong TypeScript, cú pháp mảng 'T[]' và cú pháp Generic 'Array<T>' hoàn toàn tương đương về mặt ý nghĩa kiểu dữ liệu.",
                        QuestionType.TrueFalse,
                        QuestionLevel.Easy,
                        [
                            ("Đúng", true),
                            ("Sai", false)
                        ])
                ]
            ),

            // 8. TypeScript Advanced Types
            new QuizSeedData(
                new Quiz
                {
                    Id = Quiz8TsAdvancedId,
                    Title = "TypeScript: Advanced Types & Utility Types",
                    Description = "Chinh phục hệ thống type nâng cao trong TypeScript: Partial, Required, Pick, Omit, Record, conditional types, mapped types và template literal types.",
                    Duration = 25,
                    PassedScore = 75.0,
                    IsActive = true,
                    CreateAt = SeedTimestamp,
                    UpdateAt = SeedTimestamp
                },
                [
                    CreateQuestion(
                        23,
                        "Utility type nào trong TypeScript chuyển tất cả các thuộc tính của kiểu T thành tùy chọn (optional)?",
                        QuestionType.SingleChoice,
                        QuestionLevel.Easy,
                        [
                            ("Partial<T>", true),
                            ("Required<T>", false),
                            ("Readonly<T>", false),
                            ("Record<K, T>", false)
                        ]),
                    CreateQuestion(
                        24,
                        "Để tạo một kiểu mới từ T bằng cách loại bỏ các thuộc tính được chỉ định bởi khóa K, ta sử dụng Utility type nào?",
                        QuestionType.SingleChoice,
                        QuestionLevel.Medium,
                        [
                            ("Omit<T, K>", true),
                            ("Pick<T, K>", false),
                            ("Exclude<T, U>", false),
                            ("Extract<T, U>", false)
                        ]),
                    CreateQuestion(
                        25,
                        "Toán tử 'keyof T' trong TypeScript tạo ra một Union type gồm tất cả các tên thuộc tính (keys) của kiểu T.",
                        QuestionType.TrueFalse,
                        QuestionLevel.Medium,
                        [
                            ("Đúng", true),
                            ("Sai", false)
                        ])
                ]
            ),

            // 9. ASP.NET Core Web API
            new QuizSeedData(
                new Quiz
                {
                    Id = Quiz9ApiRestId,
                    Title = "ASP.NET Core Web API & Kiến trúc RESTful",
                    Description = "Đánh giá kiến thức thiết kế Web API chuẩn RESTful trên nền tảng ASP.NET Core: HTTP verbs, Status codes, Request routing, Middleware pipeline và Model validation.",
                    Duration = 20,
                    PassedScore = 70.0,
                    IsActive = true,
                    CreateAt = SeedTimestamp,
                    UpdateAt = SeedTimestamp
                },
                [
                    CreateQuestion(
                        26,
                        "Theo chuẩn HTTP RESTful, mã trạng thái HTTP (Status Code) nào phù hợp nhất khi một tài nguyên mới được tạo thành công qua yêu cầu POST?",
                        QuestionType.SingleChoice,
                        QuestionLevel.Easy,
                        [
                            ("201 Created", true),
                            ("200 OK", false),
                            ("204 No Content", false),
                            ("202 Accepted", false)
                        ]),
                    CreateQuestion(
                        27,
                        "Những phương thức HTTP (HTTP Verbs) nào sau đây được coi là Idempotent theo đặc tả chuẩn HTTP?",
                        QuestionType.MultipleChoice,
                        QuestionLevel.Medium,
                        [
                            ("GET", true),
                            ("PUT", true),
                            ("DELETE", true),
                            ("POST", false)
                        ]),
                    CreateQuestion(
                        28,
                        "Trong ASP.NET Core, thứ tự đăng ký các Middleware trong Program.cs quyết định trực tiếp đến thứ tự xử lý của request và response.",
                        QuestionType.TrueFalse,
                        QuestionLevel.Easy,
                        [
                            ("Đúng", true),
                            ("Sai", false)
                        ])
                ]
            ),

            // 10. Web API Security & JWT
            new QuizSeedData(
                new Quiz
                {
                    Id = Quiz10ApiSecurityId,
                    Title = "Bảo mật Web API: JWT, Authentication & Authorization",
                    Description = "Chuyên đề bảo mật ứng dụng Web API hiện đại: cấu trúc và xác thực JSON Web Token (JWT), Claims-based Authorization, Role-based Access Control (RBAC), phòng chống tấn công và cấu hình CORS.",
                    Duration = 25,
                    PassedScore = 75.0,
                    IsActive = true,
                    CreateAt = SeedTimestamp,
                    UpdateAt = SeedTimestamp
                },
                [
                    CreateQuestion(
                        29,
                        "Một chuỗi JSON Web Token (JWT) tiêu chuẩn gồm bao nhiêu phần chính được phân cách bằng dấu chấm (.)?",
                        QuestionType.SingleChoice,
                        QuestionLevel.Easy,
                        [
                            ("3 (Header, Payload, Signature)", true),
                            ("2 (Header, Body)", false),
                            ("4 (Header, Claims, Signature, Hash)", false),
                            ("5 (Issuer, Subject, Claims, Hash, Signature)", false)
                        ]),
                    CreateQuestion(
                        30,
                        "Header HTTP tiêu chuẩn nào được sử dụng để gửi JWT Bearer Token trong các yêu cầu bảo vệ lên API?",
                        QuestionType.SingleChoice,
                        QuestionLevel.Easy,
                        [
                            ("Authorization", true),
                            ("Authentication", false),
                            ("X-Token", false),
                            ("Bearer", false)
                        ]),
                    CreateQuestion(
                        31,
                        "Phần Payload của JWT được mã hóa bí mật nên phía client không thể đọc được nội dung claims nếu không có Signing Key.",
                        QuestionType.TrueFalse,
                        QuestionLevel.Medium,
                        [
                            ("Đúng", false),
                            ("Sai", true)
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
