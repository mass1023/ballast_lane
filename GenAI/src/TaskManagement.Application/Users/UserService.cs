using TaskManagement.Application.Common;
using TaskManagement.Domain.Users;

namespace TaskManagement.Application.Users;

public sealed record UserDto(Guid Id, string Username, DateTimeOffset CreatedAt)
{
    public static UserDto From(User user) => new(user.Id, user.Username, user.CreatedAt);
}

public interface IUserService
{
    Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken ct = default);
    Task<UserDto> GetAsync(Guid id, CancellationToken ct = default);
}

public sealed class UserService : IUserService
{
    private readonly IUserRepository _users;

    public UserService(IUserRepository users)
    {
        _users = users;
    }

    public async Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken ct = default)
    {
        var users = await _users.ListAsync(ct);
        return users.Select(UserDto.From).ToList();
    }

    public async Task<UserDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(id, ct) ?? throw new NotFoundException("User", id);
        return UserDto.From(user);
    }
}
