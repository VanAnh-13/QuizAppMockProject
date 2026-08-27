using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace QuizApp.Api.IntegrationTests;

/// <summary>
/// Quiz CRUD round-trip, question assignment/removal via QuizQuestion ids, the
/// publish-without-questions rejection (UC-10 alt-flow 9.1).
/// </summary>
[Collection(ApiCollection.CollectionName)]
public class QuizManagementTests
{
    private readonly QuizApiFactory _factory;

    public QuizManagementTests(QuizApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> AdminClientAsync()
    {
        var client = _factory.CreateClient();
        client.WithBearer(await TestClient.LoginAsync(client, TestClient.AdminUser, TestClient.AdminPassword));
        return client;
    }

    [Fact]
    public async Task QuizCrud_RoundTrip()
    {
        var client = await AdminClientAsync();
        var title = $"CRUD quiz {Guid.NewGuid():N}";

        var created = await client.PostAsJsonAsync("/api/quizzes", new { title, description = "desc", duration = 15, isActive = false });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var quiz = await created.Content.ReadFromJsonAsync<QuizDto>();
        Assert.NotNull(quiz);

        var fetched = await client.GetFromJsonAsync<QuizDto>($"/api/quizzes/{quiz.Id}");
        Assert.Equal(title, fetched!.Title);

        var updated = await client.PutAsJsonAsync($"/api/quizzes/{quiz.Id}",
            new { id = quiz.Id, title = title + " v2", description = "desc", duration = 20, isActive = false });
        updated.EnsureSuccessStatusCode();

        var deleted = await client.DeleteAsync($"/api/quizzes/{quiz.Id}");
        deleted.EnsureSuccessStatusCode();

        var gone = await client.GetAsync($"/api/quizzes/{quiz.Id}");
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
    }

    [Fact]
    public async Task PublishQuiz_WithoutQuestions_Returns409()
    {
        var client = await AdminClientAsync();
        var created = await client.PostAsJsonAsync("/api/quizzes",
            new { title = $"NoQuestions {Guid.NewGuid():N}", description = "d", duration = 10, isActive = false });
        var quiz = await created.Content.ReadFromJsonAsync<QuizDto>();

        var publish = await client.PutAsJsonAsync($"/api/quizzes/{quiz!.Id}",
            new { id = quiz.Id, title = quiz.Title, description = quiz.Description, duration = quiz.Duration, isActive = true });

        Assert.Equal(HttpStatusCode.Conflict, publish.StatusCode);
        Assert.Contains("zero questions", await publish.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task AssignAndRemoveQuestion_UpdatesQuizQuestions()
    {
        var client = await AdminClientAsync();

        var createdQuiz = await client.PostAsJsonAsync("/api/quizzes",
            new { title = $"Assign {Guid.NewGuid():N}", description = "d", duration = 10, isActive = false });
        var quiz = await createdQuiz.Content.ReadFromJsonAsync<QuizDto>();

        var createdQuestion = await client.PostAsJsonAsync("/api/questions",
            new { content = "What is 2+2?", questionType = 1, isActive = true });
        Assert.Equal(HttpStatusCode.Created, createdQuestion.StatusCode);
        var question = await createdQuestion.Content.ReadFromJsonAsync<QuestionDto>();

        var assign = await client.PostAsJsonAsync($"/api/quizzes/{quiz!.Id}/questions",
            new { quizId = quiz.Id, questionId = question!.Id });
        Assert.Equal(HttpStatusCode.NoContent, assign.StatusCode);

        var assigned = await client.GetFromJsonAsync<List<AssignedQuestionDto>>($"/api/quizzes/{quiz.Id}/questions");
        var assignment = Assert.Single(assigned!);
        Assert.Equal(question.Id, assignment.Id);
        Assert.False(string.IsNullOrEmpty(assignment.QuizQuestionId));

        // Publishing succeeds once the quiz has a question.
        var publish = await client.PutAsJsonAsync($"/api/quizzes/{quiz.Id}",
            new { id = quiz.Id, title = quiz.Title, description = quiz.Description, duration = quiz.Duration, isActive = true });
        publish.EnsureSuccessStatusCode();

        var remove = await client.DeleteAsync($"/api/quizzes/{quiz.Id}/questions/{assignment.QuizQuestionId}");
        Assert.Equal(HttpStatusCode.NoContent, remove.StatusCode);

        var afterRemoval = await client.GetFromJsonAsync<List<AssignedQuestionDto>>($"/api/quizzes/{quiz.Id}/questions");
        Assert.Empty(afterRemoval!);
    }

    [Fact]
    public async Task Paging_Search_Sort_AreEvaluatedOnServer()
    {
        var client = await AdminClientAsync();
        var prefix = $"PT{Guid.NewGuid():N}".Substring(0, 12);

        for (var i = 1; i <= 25; i++)
        {
            var create = await client.PostAsJsonAsync("/api/quizzes",
                new { title = $"{prefix} {i:00}", description = "paging fixture", duration = 10 + i, isActive = false });
            create.EnsureSuccessStatusCode();
        }

        // page=2, pageSize=10 → 10 items, totalItems=25, totalPages=3
        var page2 = await client.GetFromJsonAsync<PagedQuiz>(
            $"/api/quizzes?page=2&pageSize=10&search={prefix}&sortBy=title&sortDir=asc");
        Assert.NotNull(page2);
        Assert.Equal(25, page2.TotalItems);
        Assert.Equal(3, page2.TotalPages);
        Assert.Equal(10, page2.Items.Count);
        Assert.Equal($"{prefix} 11", page2.Items[0].Title);

        // sort desc reverses
        var desc = await client.GetFromJsonAsync<PagedQuiz>(
            $"/api/quizzes?page=1&pageSize=5&search={prefix}&sortBy=title&sortDir=desc");
        Assert.Equal($"{prefix} 25", desc!.Items[0].Title);

        // search narrows
        var narrowed = await client.GetFromJsonAsync<PagedQuiz>($"/api/quizzes?search={prefix} 07");
        Assert.Equal(1, narrowed!.TotalItems);

        // clamping: huge pageSize capped at 100; negative page becomes 1
        var clamped = await client.GetFromJsonAsync<PagedQuiz>($"/api/quizzes?page=-5&pageSize=100000&search={prefix}");
        Assert.Equal(25, clamped!.Items.Count);
        Assert.Equal(1, clamped.Page);
        Assert.True(clamped.PageSize <= 100);
    }

    [Fact]
    public async Task DeleteQuestion_AssignedToQuiz_WarnsAndPreservesFlow()
    {
        var client = await AdminClientAsync();

        var quiz = await (await client.PostAsJsonAsync("/api/quizzes",
            new { title = $"DelWarn {Guid.NewGuid():N}", description = "d", duration = 10, isActive = false }))
            .Content.ReadFromJsonAsync<QuizDto>();
        var question = await (await client.PostAsJsonAsync("/api/questions",
            new { content = "Assigned question", questionType = 1, isActive = true }))
            .Content.ReadFromJsonAsync<QuestionDto>();

        await client.PostAsJsonAsync($"/api/quizzes/{quiz!.Id}/questions", new { quizId = quiz.Id, questionId = question!.Id });

        // UC-11 alt-flow 3.1: the delete preview warns about the assignment.
        var preview = await client.GetFromJsonAsync<DeletePreviewDto>($"/api/questions/{question.Id}/delete-preview");
        Assert.NotNull(preview);
        Assert.True(preview!.IsAssignedToQuizzes);
        Assert.Contains(quiz.Title, preview.QuizTitles);

        // Deleting removes it from the quiz (no FK errors).
        var delete = await client.DeleteAsync($"/api/questions/{question.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var remaining = await client.GetFromJsonAsync<List<AssignedQuestionDto>>($"/api/quizzes/{quiz.Id}/questions");
        Assert.Empty(remaining!);
    }

    public sealed class DeletePreviewDto
    {
        public bool IsAssignedToQuizzes { get; set; }
        public string[] QuizTitles { get; set; } = Array.Empty<string>();
        public string Warning { get; set; } = string.Empty;
    }

    public sealed class QuizDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Duration { get; set; }
        public bool IsActive { get; set; }
    }

    public sealed class QuestionDto
    {
        public string Id { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int QuestionType { get; set; }
        public bool IsActive { get; set; }
    }

    public sealed class AssignedQuestionDto
    {
        public string QuizQuestionId { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public sealed class PagedQuiz
    {
        public List<QuizDto> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
    }
}
