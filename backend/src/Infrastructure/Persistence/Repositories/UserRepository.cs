using Npgsql;
using RecordLabel.Application.Abstractions;
using RecordLabel.Application.Common;
using RecordLabel.Domain.Users;

namespace RecordLabel.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private const string Columns = "id, username, password_hash, created_at";

    private readonly NpgsqlDataSource _db;

    public UserRepository(NpgsqlDataSource dataSource)
    {
        _db = dataSource;
    }

    public async Task<User?> GetByUsername(string username, CancellationToken cancellationToken)
    {
        await using var command = _db.CreateCommand($"SELECT {Columns} FROM users WHERE lower(username) = lower(@username)");
        command.Parameters.AddWithValue("username", username);
        return await ReadSingle(command, cancellationToken);
    }

    public async Task<User?> GetById(Guid id, CancellationToken cancellationToken)
    {
        await using var command = _db.CreateCommand($"SELECT {Columns} FROM users WHERE id = @id");
        command.Parameters.AddWithValue("id", id);
        return await ReadSingle(command, cancellationToken);
    }

    public async Task Add(User user, CancellationToken cancellationToken)
    {
        await using var command = _db.CreateCommand(
            $"INSERT INTO users ({Columns}) VALUES (@id, @username, @passwordHash, @createdAt)");
        command.Parameters.AddWithValue("createdAt", user.CreatedAt);
        command.Parameters.AddWithValue("passwordHash", user.PasswordHash);
        command.Parameters.AddWithValue("username", user.Username);
        command.Parameters.AddWithValue("id", user.Id);

        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new ConflictException("That username is already taken.");
        }
    }

    private static async Task<User?> ReadSingle(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return new User
        {
            Id = reader.GetGuid(0),
            Username = reader.GetString(1),
            PasswordHash = reader.GetString(2),
            CreatedAt = reader.GetDateTime(3)
        };
    }
}
