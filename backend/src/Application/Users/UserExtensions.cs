using RecordLabel.Application.Abstractions;
using RecordLabel.Domain.Users;

namespace RecordLabel.Application.Users;

public static class UserExtensions
{
    public static UserDto ToDto(this User user)
    {
        return new UserDto(user.Id, user.Username);
    }

    public static AuthenticateResponse ToAuthenticateResponse(this User user, AccessToken token)
    {
        return new AuthenticateResponse(user.Id, user.Username, token.Token, token.ExpiresAt);
    }
}
