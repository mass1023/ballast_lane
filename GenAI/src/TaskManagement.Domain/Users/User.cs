namespace TaskManagement.Domain.Users;

public sealed record User(Guid Id, string Username, DateTimeOffset CreatedAt);
