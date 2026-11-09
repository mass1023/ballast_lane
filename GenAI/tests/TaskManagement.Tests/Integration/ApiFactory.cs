using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;

namespace TaskManagement.Tests.Integration;

public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public static readonly Guid Alice = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid Bob = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder("postgres:18-alpine").Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:TaskManagement", _db.GetConnectionString());
    }

    public async Task ResetAsync()
    {
        await using var cmd = Services.GetRequiredService<NpgsqlDataSource>().CreateCommand("DELETE FROM tasks;");
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task InitializeAsync()
    {
        await _db.StartAsync();
        _ = Services;
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _db.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "Api";
}

[Collection(ApiCollection.Name)]
public abstract class IntegrationTest : IAsyncLifetime
{
    protected IntegrationTest(ApiFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    protected ApiFactory Factory { get; }
    protected HttpClient Client { get; }
    protected static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    public Task InitializeAsync() => Factory.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;
}
