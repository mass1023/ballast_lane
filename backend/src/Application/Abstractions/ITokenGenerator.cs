using RecordLabel.Domain.Users;

namespace RecordLabel.Application.Abstractions;

public record AccessToken(string Token, DateTime ExpiresAt);

public interface ITokenGenerator
{
    AccessToken Generate(User user);
}
