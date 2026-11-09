using TaskManagement.Domain.Common;

namespace TaskManagement.Domain.Tasks;

public sealed class TaskItem
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 2000;

    private TaskItem(Guid id, string title, string? description, TaskItemStatus status, DateOnly dueDate, Guid responsableId)
    {
        Id = id;
        Title = title;
        Description = description;
        Status = status;
        DueDate = dueDate;
        ResponsableId = responsableId;
    }

    public Guid Id { get; }
    public string Title { get; private set; }
    public string? Description { get; private set; }
    public TaskItemStatus Status { get; private set; }
    public DateOnly DueDate { get; private set; }
    public Guid ResponsableId { get; private set; }

    public static TaskItem Create(string? title, string? description, DateOnly? dueDate, Guid responsableId, DateOnly today)
    {
        var errors = Validate(title, description, TaskItemStatus.Pending, dueDate, responsableId);

        if (dueDate is not null && dueDate < today)
            errors.Add("dueDate", ["Due date cannot be in the past."]);

        ThrowIfAny(errors);

        return new TaskItem(Guid.CreateVersion7(), title!.Trim(), Normalize(description), TaskItemStatus.Pending, dueDate!.Value, responsableId);
    }

    public static TaskItem Load(Guid id, string title, string? description, TaskItemStatus status, DateOnly dueDate, Guid responsableId) =>
        new(id, title, description, status, dueDate, responsableId);

    public void Update(string? title, string? description, TaskItemStatus status, DateOnly? dueDate, Guid responsableId)
    {
        ThrowIfAny(Validate(title, description, status, dueDate, responsableId));

        Title = title!.Trim();
        Description = Normalize(description);
        Status = status;
        DueDate = dueDate!.Value;
        ResponsableId = responsableId;
    }

    private static Dictionary<string, string[]> Validate(string? title, string? description, TaskItemStatus status, DateOnly? dueDate, Guid responsableId)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(title))
            errors.Add("title", ["Title is required."]);
        else if (title.Trim().Length > TitleMaxLength)
            errors.Add("title", [$"Title cannot exceed {TitleMaxLength} characters."]);

        if (description is not null && description.Trim().Length > DescriptionMaxLength)
            errors.Add("description", [$"Description cannot exceed {DescriptionMaxLength} characters."]);

        if (!Enum.IsDefined(status))
            errors.Add("status", ["Status must be Pending, InProgress or Done."]);

        if (dueDate is null)
            errors.Add("dueDate", ["Due date is required."]);

        if (responsableId == Guid.Empty)
            errors.Add("responsableId", ["Responsable is required."]);

        return errors;
    }

    private static void ThrowIfAny(Dictionary<string, string[]> errors)
    {
        if (errors.Count > 0)
            throw new DomainValidationException(errors);
    }

    private static string? Normalize(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
