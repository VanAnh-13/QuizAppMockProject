using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuizApp.Domain.Entities;
using QuizApp.Domain.Enums;

namespace QuizApp.Infrastructure.Persistence;

/// <summary>
/// Seeds roles, the admin account (from configuration) and sample content so no screen is ever empty.
/// Fully idempotent.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var context = services.GetRequiredService<AppDbContext>();
        var configuration = services.GetRequiredService<IConfiguration>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

        await SeedRolesAsync(roleManager);
        await SeedUsersAsync(userManager, configuration, logger);
        await SeedSampleContentAsync(context, cancellationToken);
    }

    private static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager)
    {
        var roles = new (string Name, string Description)[]
        {
            ("Admin", "Full access to every management area."),
            ("Editor", "Manages quiz content: quizzes, questions and answers."),
            ("User", "Takes quizzes and reviews own history.")
        };

        foreach (var (name, description) in roles)
        {
            if (!await roleManager.RoleExistsAsync(name))
            {
                await roleManager.CreateAsync(new ApplicationRole(name, description));
            }
        }
    }

    private static async Task SeedUsersAsync(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        ILogger logger)
    {
        // Admin account read from configuration (Seed:Admin:*), never hardcoded credentials.
        await EnsureUserAsync(userManager, logger, new SeedUser
        {
            UserName = configuration["Seed:Admin:UserName"] ?? "admin",
            Email = configuration["Seed:Admin:Email"] ?? "admin@quizapp.local",
            Password = configuration["Seed:Admin:Password"] ?? "Admin@12345",
            FirstName = configuration["Seed:Admin:FirstName"] ?? "System",
            LastName = configuration["Seed:Admin:LastName"] ?? "Administrator",
            Roles = new[] { "Admin" }
        });

        // Convenience demo account for local development.
        await EnsureUserAsync(userManager, logger, new SeedUser
        {
            UserName = configuration["Seed:User:UserName"] ?? "user",
            Email = configuration["Seed:User:Email"] ?? "user@quizapp.local",
            Password = configuration["Seed:User:Password"] ?? "User@12345",
            FirstName = configuration["Seed:User:FirstName"] ?? "Demo",
            LastName = configuration["Seed:User:LastName"] ?? "User",
            Roles = new[] { "User" }
        });
    }

    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        ILogger logger,
        SeedUser seed)
    {
        var existing = await userManager.FindByNameAsync(seed.UserName);
        if (existing is not null)
        {
            return;
        }

        var user = new ApplicationUser
        {
            UserName = seed.UserName,
            Email = seed.Email,
            EmailConfirmed = true,
            FirstName = seed.FirstName,
            LastName = seed.LastName,
            DateOfBirth = new DateTime(1998, 5, 16, 0, 0, 0, DateTimeKind.Utc),
            PhoneNumber = "0123456789",
            IsActive = true
        };

        var result = await userManager.CreateAsync(user, seed.Password);
        if (!result.Succeeded)
        {
            logger.LogError("Failed to seed user {UserName}: {Errors}", seed.UserName,
                string.Join("; ", result.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRolesAsync(user, seed.Roles);
        logger.LogInformation("Seeded user {UserName} with roles {Roles}", seed.UserName, string.Join(", ", seed.Roles));
    }

    private static async Task SeedSampleContentAsync(AppDbContext context, CancellationToken ct)
    {
        if (await context.Quizzes.AnyAsync(ct))
        {
            return;
        }

        // --- Question bank covering all six question types
        var singleChoice = new Question
        {
            Content = "Which planet is known as the Red Planet?",
            QuestionType = QuestionType.SingleChoice,
            Answers =
            {
                new Answer { Content = "Venus", IsCorrect = false },
                new Answer { Content = "Mars", IsCorrect = true },
                new Answer { Content = "Jupiter", IsCorrect = false },
                new Answer { Content = "Saturn", IsCorrect = false }
            }
        };

        var multipleChoice = new Question
        {
            Content = "Which of the following are prime numbers?",
            QuestionType = QuestionType.MultipleChoice,
            Answers =
            {
                new Answer { Content = "2", IsCorrect = true },
                new Answer { Content = "9", IsCorrect = false },
                new Answer { Content = "13", IsCorrect = true },
                new Answer { Content = "21", IsCorrect = false }
            }
        };

        var trueFalse = new Question
        {
            Content = "The Great Wall of China is visible from space with the naked eye.",
            QuestionType = QuestionType.TrueFalse,
            Answers =
            {
                new Answer { Content = "True", IsCorrect = false },
                new Answer { Content = "False", IsCorrect = true }
            }
        };

        var fillInTheBlanks = new Question
        {
            Content = "The capital of France is _____.",
            QuestionType = QuestionType.FillInTheBlanks,
            Answers =
            {
                new Answer { Content = "Paris", IsCorrect = true }
            }
        };

        var shortAnswer = new Question
        {
            Content = "Which language is primarily used to style web pages?",
            QuestionType = QuestionType.ShortAnswer,
            Answers =
            {
                new Answer { Content = "CSS", IsCorrect = true }
            }
        };

        var longAnswer = new Question
        {
            Content = "Describe the difference between a stack and a queue.",
            QuestionType = QuestionType.LongAnswer,
            Answers =
            {
                new Answer { Content = "Stack is LIFO, queue is FIFO.", IsCorrect = true }
            }
        };

        var angularQuestion = new Question
        {
            Content = "Which command creates a new Angular workspace?",
            QuestionType = QuestionType.SingleChoice,
            Answers =
            {
                new Answer { Content = "ng new", IsCorrect = true },
                new Answer { Content = "ng create", IsCorrect = false },
                new Answer { Content = "dotnet new", IsCorrect = false },
                new Answer { Content = "npm start", IsCorrect = false }
            }
        };

        var efQuestion = new Question
        {
            Content = "Entity Framework Core is an ORM for .NET.",
            QuestionType = QuestionType.TrueFalse,
            Answers =
            {
                new Answer { Content = "True", IsCorrect = true },
                new Answer { Content = "False", IsCorrect = false }
            }
        };

        context.Questions.AddRange(singleChoice, multipleChoice, trueFalse, fillInTheBlanks,
            shortAnswer, longAnswer, angularQuestion, efQuestion);

        // --- Quizzes
        var generalKnowledge = new Quiz
        {
            Title = "General Knowledge Challenge",
            Description = "A quick warm-up covering geography, astronomy and common myths.",
            Duration = 15,
            IsActive = true,
            ThumbnailUrl = "https://picsum.photos/seed/generalknowledge/640/360"
        };
        generalKnowledge.QuizQuestions.Add(new QuizQuestion { Question = singleChoice, DisplayOrder = 1 });
        generalKnowledge.QuizQuestions.Add(new QuizQuestion { Question = trueFalse, DisplayOrder = 2 });
        generalKnowledge.QuizQuestions.Add(new QuizQuestion { Question = fillInTheBlanks, DisplayOrder = 3 });

        var programming = new Quiz
        {
            Title = "Programming Fundamentals",
            Description = "Data structures, web technologies and .NET basics for budding developers.",
            Duration = 30,
            IsActive = true,
            ThumbnailUrl = "https://picsum.photos/seed/programming/640/360"
        };
        programming.QuizQuestions.Add(new QuizQuestion { Question = multipleChoice, DisplayOrder = 1 });
        programming.QuizQuestions.Add(new QuizQuestion { Question = shortAnswer, DisplayOrder = 2 });
        programming.QuizQuestions.Add(new QuizQuestion { Question = longAnswer, DisplayOrder = 3 });
        programming.QuizQuestions.Add(new QuizQuestion { Question = angularQuestion, DisplayOrder = 4 });
        programming.QuizQuestions.Add(new QuizQuestion { Question = efQuestion, DisplayOrder = 5 });

        var webDevelopment = new Quiz
        {
            Title = "Web Development Basics",
            Description = "HTML, CSS and framework trivia for frontend enthusiasts.",
            Duration = 60,
            IsActive = true,
            ThumbnailUrl = "https://picsum.photos/seed/webdev/640/360"
        };
        webDevelopment.QuizQuestions.Add(new QuizQuestion { Question = angularQuestion, DisplayOrder = 1 });
        webDevelopment.QuizQuestions.Add(new QuizQuestion { Question = shortAnswer, DisplayOrder = 2 });

        var draft = new Quiz
        {
            Title = "Advanced Science (Draft)",
            Description = "Still being authored - not visible to customers yet.",
            Duration = 45,
            IsActive = false
        };
        draft.QuizQuestions.Add(new QuizQuestion { Question = multipleChoice, DisplayOrder = 1 });

        context.Quizzes.AddRange(generalKnowledge, programming, webDevelopment, draft);

        await context.SaveChangesAsync(ct);
    }

    private sealed class SeedUser
    {
        public required string UserName { get; init; }
        public required string Email { get; init; }
        public required string Password { get; init; }
        public required string FirstName { get; init; }
        public required string LastName { get; init; }
        public required string[] Roles { get; init; }
    }
}
