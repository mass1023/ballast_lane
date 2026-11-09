using Npgsql;

namespace RecordLabel.Infrastructure.Persistence;

public class DatabaseInitializer
{
    private const string SchemaResource = "RecordLabel.Infrastructure.Persistence.Scripts.schema.sql";

    private readonly NpgsqlDataSource _db;

    public DatabaseInitializer(NpgsqlDataSource dataSource)
    {
        _db = dataSource;
    }

    public async Task Initialize(CancellationToken cancellationToken = default)
    {
        await using var stream = typeof(DatabaseInitializer).Assembly.GetManifestResourceStream(SchemaResource)
            ?? throw new InvalidOperationException($"Embedded resource '{SchemaResource}' was not found.");
        using var reader = new StreamReader(stream);
        var schema = await reader.ReadToEndAsync(cancellationToken);

        await using var command = _db.CreateCommand(schema);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
