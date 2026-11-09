using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using RecordLabel.Application.Users;

namespace RecordLabel.IntegrationTests.Api;

public sealed class AuthEndpointTests(PostgresFixture fixture) : ApiTestBase(fixture)
{
    [Fact]
    public async Task Register_ValidUser_Returns201AndTheUserCanLogIn()
    {
        var register = await Client.PostAsJsonAsync("/api/auth/register", new { username = "newuser", password = "secret" });

        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        var user = await register.Content.ReadFromJsonAsync<UserDto>(Json);
        Assert.Equal("newuser", user!.Username);

        var login = await Client.PostAsJsonAsync("/api/auth/login", new { username = "newuser", password = "secret" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task Register_TakenUsername_Returns409()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/register", new { username = "ADMIN", password = "secret" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidInput_Returns400WithFieldErrors()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/register", new { username = "ab", password = "123" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(Json);
        Assert.Contains("username", problem!.Errors.Keys);
        Assert.Contains("password", problem.Errors.Keys);
    }

    [Fact]
    public async Task Login_SeededAdmin_ReturnsAToken()
    {
        var login = await LoginAsAdmin();

        Assert.Equal(AdminUsername, login.Username);
        Assert.False(string.IsNullOrEmpty(login.Token));
        Assert.True(login.ExpiresAt > DateTime.UtcNow);
    }

    [Theory]
    [InlineData("admin", "wrong")]
    [InlineData("nobody", "admin")]
    public async Task Login_BadCredentials_Returns401(string username, string password)
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login", new { username, password });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401()
    {
        var response = await Client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithTamperedToken_Returns401()
    {
        var login = await LoginAsAdmin();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token[..^2] + "xx");

        var response = await Client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithToken_ReturnsTheLoggedInUser()
    {
        var login = await LoginAsAdmin();

        var me = await Client.GetFromJsonAsync<UserDto>("/api/auth/me", Json);

        Assert.Equal(new UserDto(login.UserId, AdminUsername), me);
    }
}
