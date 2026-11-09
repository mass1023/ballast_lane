using Dapper;
using Npgsql;

namespace TaskManagement.Infrastructure.Persistence;

public sealed class DatabaseInitializer
{
    private readonly NpgsqlDataSource _dataSource;

    public DatabaseInitializer(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await using var stream = typeof(DatabaseInitializer).Assembly
            .GetManifestResourceStream("TaskManagement.Infrastructure.Persistence.schema.sql")!;
        using var reader = new StreamReader(stream);
        var sql = await reader.ReadToEndAsync(ct);

        await using var connection = await _dataSource.OpenConnectionAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: ct));
    }
}
