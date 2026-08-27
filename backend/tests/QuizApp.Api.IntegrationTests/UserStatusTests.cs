using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace QuizApp.Api.IntegrationTests;

/// <summary>FR-5: a user with isActive = false cannot log in.</summary>
[Collection(ApiCollection.CollectionName)]
public class UserStatusTests
{
    private readonly QuizApiFactory _factory;

    public UserStatusTests(QuizApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task DeactivatedUser_CannotLogin()
    {
        var admin = _factory.CreateClient();
        admin.WithBearer(await TestClient.LoginAsync(admin, TestClient.AdminUser, TestClient.AdminPassword));

        var userName = $"deactivated_{Guid.NewGuid():N}".Substring(0, 24);
        var create = await admin.PostAsJsonAsync("/api/users", new
        {
            firstName = "De", lastName = "Activated",
            email = $"{userName}@test.local", userName,
            phoneNumber = "0900000001", dateOfBirth = "1999-01-01",
            password = "Deact@12345", confirmPassword = "Deact@12345", isActive = true
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var user = await create.Content.ReadFromJsonAsync<UserDto>();
        Assert.NotNull(user);

        // Active user can log in.
        var okLogin = await admin.PostAsJsonAsync("/api/auth/login", new { userName, password = "Deact@12345" });
        Assert.Equal(HttpStatusCode.OK, okLogin.StatusCode);

        // Admin deactivates the account.
        var deactivate = await admin.PutAsJsonAsync($"/api/users/{user!.Id}/status", new { isActive = false });
        deactivate.EnsureSuccessStatusCode();

        // Login is now rejected with 403 and a clear message.
        var blockedLogin = await admin.PostAsJsonAsync("/api/auth/login", new { userName, password = "Deact@12345" });
        Assert.Equal(HttpStatusCode.Forbidden, blockedLogin.StatusCode);
        Assert.Contains("deactivated", await blockedLogin.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task UserManagement_OnlyAdmin()
    {
        var client = _factory.CreateClient();
        client.WithBearer(await TestClient.LoginAsync(client, TestClient.DemoUser, TestClient.DemoPassword));

        var response = await client.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private sealed class UserDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
