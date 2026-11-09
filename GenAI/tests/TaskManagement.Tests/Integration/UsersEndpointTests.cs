using System.Net;
using System.Net.Http.Json;

namespace TaskManagement.Tests.Integration;

public class UsersEndpointTests : IntegrationTest
{
    public UsersEndpointTests(ApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task List_ReturnsSeededUsers()
    {
        var users = await Client.GetFromJsonAsync<List<UserResponse>>("/api/users");

        Assert.Equal(["alice", "bob", "carol"], users!.Select(u => u.Username).ToArray());
    }

    [Fact]
    public async Task Get_ReturnsUser()
    {
        var user = await Client.GetFromJsonAsync<UserResponse>($"/api/users/{ApiFactory.Bob}");

        Assert.Equal("bob", user!.Username);
    }

    [Fact]
    public async Task Get_Unknown_Returns404()
    {
        var response = await Client.GetAsync($"/api/users/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UsersAreReadOnly()
    {
        var response = await Client.PostAsJsonAsync("/api/users", new { username = "mallory" });

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    private sealed record UserResponse(Guid Id, string Username, DateTimeOffset CreatedAt);
}
