namespace TaskManagement.Application.Common;

public sealed class NotFoundException : Exception
{
    public NotFoundException(string resource, Guid id)
        : base($"{resource} '{id}' was not found.")
    {
    }
}
