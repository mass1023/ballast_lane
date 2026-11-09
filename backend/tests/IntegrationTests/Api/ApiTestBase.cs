using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using RecordLabel.Application.Users;

namespace RecordLabel.IntegrationTests.Api;

[Collection(PostgresCollection.Name)]
public abstract class ApiTestBase(PostgresFixture fixture) : IAsyncLifetime
{
    public const string AdminUsername = "admin";
    public const string AdminPassword = "admin";

    protected static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private WebApplicationFactory<Program> _factory = null!;

    protected HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await fixture.ResetDatabaseAsync();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseSetting("ConnectionStrings:RecordLabel", fixture.ConnectionString));
        Client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        await _factory.DisposeAsync();
    }

    protected async Task<AuthenticateResponse> LoginAsAdmin()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login", new { username = AdminUsername, password = AdminPassword });
        response.EnsureSuccessStatusCode();

        var login = (await response.Content.ReadFromJsonAsync<AuthenticateResponse>(Json))!;
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);
        return login;
    }
}
