using RecordLabel.Application.Abstractions;
using RecordLabel.Application.Common;
using RecordLabel.Domain.Users;

namespace RecordLabel.Application.Users;

public interface IUserService
{
    Task<UserDto> Register(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthenticateResponse?> Authenticate(AuthenticateRequest request, CancellationToken cancellationToken = default);
    Task<UserDto> GetById(Guid id, CancellationToken cancellationToken = default);
}

public class UserService : IUserService
{
    public const int MinUsernameLength = 3;
    public const int MaxUsernameLength = 50;
    public const int MinPasswordLength = 5;

    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenGenerator _tokens;
    private readonly TimeProvider _clock;

    public UserService(IUserRepository userRepository, IPasswordHasher passwordHasher, ITokenGenerator tokenGenerator, TimeProvider timeProvider)
    {
        _clock = timeProvider;
        _tokens = tokenGenerator;
        _hasher = passwordHasher;
        _users = userRepository;
    }

    public async Task<UserDto> Register(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var username = request.Username?.Trim() ?? string.Empty;
        var password = request.Password ?? string.Empty;

        var errors = new Dictionary<string, string[]>();
        if (username.Length < MinUsernameLength || username.Length > MaxUsernameLength)
            errors["username"] = [$"Username must be between {MinUsernameLength} and {MaxUsernameLength} characters."];
        if (password.Length < MinPasswordLength)
            errors["password"] = [$"Password must be at least {MinPasswordLength} characters."];
        if (errors.Count > 0)
            throw new ValidationException(errors);

        if (await _users.GetByUsername(username, cancellationToken) != null)
            throw new ConflictException("That username is already taken.");

        var user = new User
        {
            Id = Guid.CreateVersion7(),
            Username = username,
            PasswordHash = _hasher.Hash(password),
            CreatedAt = _clock.UtcNowInMicroseconds()
        };

        await _users.Add(user, cancellationToken);
        return user.ToDto();
    }

    public async Task<AuthenticateResponse?> Authenticate(AuthenticateRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrEmpty(request.Password))
            return null;

        var user = await _users.GetByUsername(request.Username.Trim(), cancellationToken);
        if (user is null || !_hasher.Verify(user.PasswordHash, request.Password))
            return null;

        return user.ToAuthenticateResponse(_tokens.Generate(user));
    }

    public async Task<UserDto> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetById(id, cancellationToken);
        if (user is null)
            throw new NotFoundException($"User {id} was not found.");
        return user.ToDto();
    }
}
