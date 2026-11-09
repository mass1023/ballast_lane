namespace TaskManagement.Domain.Users;

public interface IUserRepository
{
    Task<IReadOnlyList<User>> ListAsync(CancellationToken ct = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
}
