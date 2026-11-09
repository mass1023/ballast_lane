using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using RecordLabel.Application;
using RecordLabel.Infrastructure;
using RecordLabel.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace RecordLabel.IntegrationTests;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18-alpine").Build();

    public ServiceProvider Services { get; private set; } = null!;

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:RecordLabel"] = ConnectionString
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddInfrastructure(configuration);
        Services = services.BuildServiceProvider(validateScopes: true);

        await Services.GetRequiredService<DatabaseInitializer>().Initialize();
    }

    public async Task ResetDatabaseAsync()
    {
        await using var command = Services.GetRequiredService<NpgsqlDataSource>().CreateCommand("TRUNCATE users, releases;");
        await command.ExecuteNonQueryAsync();
    }

    public async Task DisposeAsync()
    {
        await Services.DisposeAsync();
        await _container.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "Postgres";
}
