using RecordLabel.Domain.Users;

namespace RecordLabel.Application.Abstractions;

public interface IUserRepository
{
    Task<User?> GetByUsername(string username, CancellationToken cancellationToken);
    Task<User?> GetById(Guid id, CancellationToken cancellationToken);
    Task Add(User user, CancellationToken cancellationToken);
}
