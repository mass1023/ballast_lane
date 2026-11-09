using Dapper;
using Npgsql;
using TaskManagement.Domain.Users;

namespace TaskManagement.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private const string SelectColumns = "SELECT id, username, created_at AS CreatedAt FROM users";

    private readonly NpgsqlDataSource _dataSource;

    public UserRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<IReadOnlyList<User>> ListAsync(CancellationToken ct = default)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(ct);
        var rows = await connection.QueryAsync<UserRow>(new CommandDefinition(
            SelectColumns + " ORDER BY username", cancellationToken: ct));

        return rows.Select(r => r.ToDomain()).ToList();
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(ct);
        var row = await connection.QuerySingleOrDefaultAsync<UserRow>(new CommandDefinition(
            SelectColumns + " WHERE id = @Id", new { Id = id }, cancellationToken: ct));

        return row?.ToDomain();
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(ct);
        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            "SELECT EXISTS (SELECT 1 FROM users WHERE id = @Id)", new { Id = id }, cancellationToken: ct));
    }

    private sealed class UserRow
    {
        public Guid Id { get; init; }
        public string Username { get; init; } = "";
        public DateTime CreatedAt { get; init; }

        public User ToDomain() => new(Id, Username, new DateTimeOffset(DateTime.SpecifyKind(CreatedAt, DateTimeKind.Utc)));
    }
}
