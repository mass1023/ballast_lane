using TaskManagement.Application.Common;
using TaskManagement.Domain.Common;
using TaskManagement.Domain.Tasks;
using TaskManagement.Domain.Users;

namespace TaskManagement.Application.Tasks;

public interface ITaskService
{
    Task<IReadOnlyList<TaskDto>> ListAsync(TaskItemStatus? status, Guid? responsableId, CancellationToken ct = default);
    Task<TaskDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<TaskDto> CreateAsync(CreateTaskRequest request, CancellationToken ct = default);
    Task<TaskDto> UpdateAsync(Guid id, UpdateTaskRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

public sealed class TaskService : ITaskService
{
    private readonly ITaskRepository _tasks;
    private readonly IUserRepository _users;
    private readonly TimeProvider _time;

    public TaskService(ITaskRepository tasks, IUserRepository users, TimeProvider time)
    {
        _tasks = tasks;
        _users = users;
        _time = time;
    }

    public async Task<IReadOnlyList<TaskDto>> ListAsync(TaskItemStatus? status, Guid? responsableId, CancellationToken ct = default)
    {
        var tasks = await _tasks.ListAsync(status, responsableId, ct);
        return tasks.Select(TaskDto.From).ToList();
    }

    public async Task<TaskDto> GetAsync(Guid id, CancellationToken ct = default) =>
        TaskDto.From(await Find(id, ct));

    public async Task<TaskDto> CreateAsync(CreateTaskRequest request, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(_time.GetUtcNow().UtcDateTime);
        var task = TaskItem.Create(request.Title, request.Description, request.DueDate, request.ResponsableId ?? Guid.Empty, today);

        await EnsureUserExists(task.ResponsableId, ct);
        await _tasks.AddAsync(task, ct);

        return TaskDto.From(task);
    }

    public async Task<TaskDto> UpdateAsync(Guid id, UpdateTaskRequest request, CancellationToken ct = default)
    {
        var task = await Find(id, ct);

        if (request.Status is null)
            throw new DomainValidationException("status", "Status is required.");

        task.Update(request.Title, request.Description, request.Status.Value, request.DueDate, request.ResponsableId ?? Guid.Empty);

        await EnsureUserExists(task.ResponsableId, ct);
        await _tasks.UpdateAsync(task, ct);

        return TaskDto.From(task);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        if (!await _tasks.DeleteAsync(id, ct))
            throw new NotFoundException("Task", id);
    }

    private async Task<TaskItem> Find(Guid id, CancellationToken ct) =>
        await _tasks.GetByIdAsync(id, ct) ?? throw new NotFoundException("Task", id);

    private async Task EnsureUserExists(Guid userId, CancellationToken ct)
    {
        if (!await _users.ExistsAsync(userId, ct))
            throw new DomainValidationException("responsableId", $"User '{userId}' does not exist.");
    }
}
