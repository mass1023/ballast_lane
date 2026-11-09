namespace TaskManagement.Domain.Tasks;

public interface ITaskRepository
{
    Task<IReadOnlyList<TaskItem>> ListAsync(TaskItemStatus? status, Guid? responsableId, CancellationToken ct = default);
    Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(TaskItem task, CancellationToken ct = default);
    Task UpdateAsync(TaskItem task, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
