using TaskManagement.Domain.Tasks;

namespace TaskManagement.Application.Tasks;

public sealed record TaskDto(Guid Id, string Title, string? Description, string Status, DateOnly DueDate, Guid ResponsableId)
{
    public static TaskDto From(TaskItem task) =>
        new(task.Id, task.Title, task.Description, task.Status.ToString(), task.DueDate, task.ResponsableId);
}

public sealed record CreateTaskRequest(string? Title, string? Description, DateOnly? DueDate, Guid? ResponsableId);

public sealed record UpdateTaskRequest(string? Title, string? Description, TaskItemStatus? Status, DateOnly? DueDate, Guid? ResponsableId);
