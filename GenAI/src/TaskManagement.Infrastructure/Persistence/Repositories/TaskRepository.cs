using Dapper;
using Npgsql;
using TaskManagement.Domain.Tasks;

namespace TaskManagement.Infrastructure.Persistence.Repositories;

public sealed class TaskRepository : ITaskRepository
{
    private const string SelectColumns =
        "SELECT id, title, description, status, due_date AS DueDate, responsable_id AS ResponsableId FROM tasks";

    private readonly NpgsqlDataSource _dataSource;

    public TaskRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<IReadOnlyList<TaskItem>> ListAsync(TaskItemStatus? status, Guid? responsableId, CancellationToken ct = default)
    {
        const string sql = SelectColumns + """

             WHERE (@Status::text IS NULL OR status = @Status)
               AND (@ResponsableId::uuid IS NULL OR responsable_id = @ResponsableId)
             ORDER BY due_date, title
            """;

        await using var connection = await _dataSource.OpenConnectionAsync(ct);
        var rows = await connection.QueryAsync<TaskRow>(new CommandDefinition(
            sql, new { Status = status?.ToString(), ResponsableId = responsableId }, cancellationToken: ct));

        return rows.Select(r => r.ToDomain()).ToList();
    }

    public async Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(ct);
        var row = await connection.QuerySingleOrDefaultAsync<TaskRow>(new CommandDefinition(
            SelectColumns + " WHERE id = @Id", new { Id = id }, cancellationToken: ct));

        return row?.ToDomain();
    }

    public async Task AddAsync(TaskItem task, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO tasks (id, title, description, status, due_date, responsable_id)
            VALUES (@Id, @Title, @Description, @Status, @DueDate, @ResponsableId)
            """;

        await using var connection = await _dataSource.OpenConnectionAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql, ToParameters(task), cancellationToken: ct));
    }

    public async Task UpdateAsync(TaskItem task, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE tasks
               SET title = @Title,
                   description = @Description,
                   status = @Status,
                   due_date = @DueDate,
                   responsable_id = @ResponsableId
             WHERE id = @Id
            """;

        await using var connection = await _dataSource.OpenConnectionAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql, ToParameters(task), cancellationToken: ct));
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(ct);
        var affected = await connection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM tasks WHERE id = @Id", new { Id = id }, cancellationToken: ct));

        return affected > 0;
    }

    private static object ToParameters(TaskItem task) => new
    {
        task.Id,
        task.Title,
        task.Description,
        Status = task.Status.ToString(),
        task.DueDate,
        task.ResponsableId
    };

    private sealed class TaskRow
    {
        public Guid Id { get; init; }
        public string Title { get; init; } = "";
        public string? Description { get; init; }
        public string Status { get; init; } = "";
        public DateOnly DueDate { get; init; }
        public Guid ResponsableId { get; init; }

        public TaskItem ToDomain() => TaskItem.Load(
            Id, Title, Description, Enum.Parse<TaskItemStatus>(Status), DueDate, ResponsableId);
    }
}
