using Npgsql;
using RecordLabel.Application.Abstractions;
using RecordLabel.Application.Common;
using RecordLabel.Domain.Releases;

namespace RecordLabel.Infrastructure.Persistence.Repositories;

public class ReleaseRepository : IReleaseRepository
{
    private const string Columns = "id, title, artist, release_year, format, created_at, updated_at";
    private const string SearchFilter = "WHERE title ILIKE @pattern OR artist ILIKE @pattern";

    private readonly NpgsqlDataSource _db;

    public ReleaseRepository(NpgsqlDataSource dataSource)
    {
        _db = dataSource;
    }

    public async Task<PagedResult<Release>> Search(string? search, int page, int pageSize, CancellationToken cancellationToken)
    {
        var filter = search is null ? string.Empty : SearchFilter;
        var pattern = search is null ? null : $"%{EscapeLikePattern(search)}%";

        await using var batch = _db.CreateBatch();

        var count = new NpgsqlBatchCommand($"SELECT COUNT(*) FROM releases {filter}");
        var items = new NpgsqlBatchCommand(
            $"SELECT {Columns} FROM releases {filter} ORDER BY created_at DESC, id DESC LIMIT @limit OFFSET @offset");
        if (pattern is not null)
        {
            count.Parameters.AddWithValue("pattern", pattern);
            items.Parameters.AddWithValue("pattern", pattern);
        }
        items.Parameters.AddWithValue("limit", pageSize);
        items.Parameters.AddWithValue("offset", (page - 1) * pageSize);
        batch.BatchCommands.Add(count);
        batch.BatchCommands.Add(items);

        await using var reader = await batch.ExecuteReaderAsync(cancellationToken);

        await reader.ReadAsync(cancellationToken);
        var totalCount = (int)reader.GetInt64(0);

        await reader.NextResultAsync(cancellationToken);
        var releases = new List<Release>();
        while (await reader.ReadAsync(cancellationToken))
            releases.Add(Map(reader));

        return new PagedResult<Release>(releases, totalCount, page, pageSize);
    }

    public async Task<Release?> GetById(Guid id, CancellationToken cancellationToken)
    {
        await using var command = _db.CreateCommand($"SELECT {Columns} FROM releases WHERE id = @id");
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    public async Task Add(Release release, CancellationToken cancellationToken)
    {
        await using var command = _db.CreateCommand(
            $"INSERT INTO releases ({Columns}) VALUES (@id, @title, @artist, @releaseYear, @format, @createdAt, @updatedAt)");
        AddParameters(command, release);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> Update(Release release, CancellationToken cancellationToken)
    {
        await using var command = _db.CreateCommand(
            """
            UPDATE releases
            SET title = @title, artist = @artist, release_year = @releaseYear, format = @format, updated_at = @updatedAt
            WHERE id = @id
            """);
        AddParameters(command, release);
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    public async Task<bool> Delete(Guid id, CancellationToken cancellationToken)
    {
        await using var command = _db.CreateCommand("DELETE FROM releases WHERE id = @id");
        command.Parameters.AddWithValue("id", id);
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    private static void AddParameters(NpgsqlCommand command, Release release)
    {
        command.Parameters.AddWithValue("id", release.Id);
        command.Parameters.AddWithValue("updatedAt", release.UpdatedAt);
        command.Parameters.AddWithValue("createdAt", release.CreatedAt);
        command.Parameters.AddWithValue("format", release.Format.ToString());
        command.Parameters.AddWithValue("releaseYear", release.ReleaseYear);
        command.Parameters.AddWithValue("artist", release.Artist);
        command.Parameters.AddWithValue("title", release.Title);
    }

    private static Release Map(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetGuid(0),
        Title = reader.GetString(1),
        Artist = reader.GetString(2),
        ReleaseYear = reader.GetInt32(3),
        Format = Enum.Parse<ReleaseFormat>(reader.GetString(4)),
        CreatedAt = reader.GetDateTime(5),
        UpdatedAt = reader.GetDateTime(6)
    };

    private static string EscapeLikePattern(string value) =>
        value.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");
}
