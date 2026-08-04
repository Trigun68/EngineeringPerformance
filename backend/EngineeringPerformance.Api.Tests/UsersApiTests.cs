using System.Net;
using System.Net.Http.Json;

namespace EngineeringPerformance.Api.Tests;

public sealed class UsersApiTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly bool _sqlAvailable;

    public UsersApiTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _sqlAvailable = IntegrationTestAuth.EnsureSqlAvailable(_client);
    }

    [Fact]
    public async Task GetUsers_ReturnsAdmin_WhenAuthenticatedAsAdmin()
    {
        if (!_sqlAvailable)
            return;

        var response = await _client.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var users = await response.Content.ReadFromJsonAsync<List<UserListItem>>();
        Assert.NotNull(users);
        Assert.Contains(users!, u => u.Username == "admin");
    }

    [Fact]
    public async Task GetRoles_ReturnsAdministratorRole()
    {
        if (!_sqlAvailable)
            return;

        var response = await _client.GetAsync("/api/users/roles");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var roles = await response.Content.ReadFromJsonAsync<List<RoleListItem>>();
        Assert.NotNull(roles);
        Assert.Contains(roles!, r => r.Name == "SuperAdmin");
    }

    [Fact]
    public async Task ChangePassword_WrongOldPassword_Returns400()
    {
        if (!_sqlAvailable)
            return;

        var response = await _client.PostAsJsonAsync(
            "/api/users/me/change-password",
            new
            {
                oldPassword = "wrong-password",
                newPassword = "new-password-123",
                confirmPassword = "new-password-123",
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private sealed record UserListItem(int Id, string Username, string DisplayName);
    private sealed record RoleListItem(int Id, string Name);
}
