using Moq;
using RecordLabel.Application.Abstractions;
using RecordLabel.Application.Common;
using RecordLabel.Application.Users;
using RecordLabel.Domain.Users;

namespace RecordLabel.UnitTests.Users;

public class UserServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 9, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<ITokenGenerator> _tokenGenerator = new();
    private readonly Mock<TimeProvider> _timeProvider = new();
    private readonly UserService _service;

    public UserServiceTests()
    {
        _timeProvider.Setup(x => x.GetUtcNow()).Returns(Now);
        _passwordHasher.Setup(x => x.Hash(It.IsAny<string>())).Returns<string>(password => $"hashed:{password}");
        _passwordHasher.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((hash, password) => hash == $"hashed:{password}");
        _service = new UserService(_userRepository.Object, _passwordHasher.Object, _tokenGenerator.Object, _timeProvider.Object);
    }

    private static User ExistingUser() => new()
    {
        Id = Guid.NewGuid(),
        Username = "admin",
        PasswordHash = "hashed:admin",
        CreatedAt = Now.UtcDateTime
    };

    private async Task<ValidationException> AssertRegisterIsRejected(RegisterRequest request)
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(() => _service.Register(request));
        _userRepository.Verify(x => x.Add(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        return exception;
    }

    [Fact]
    public async Task Register_ValidRequest_SavesUserWithHashedPassword()
    {
        User? saved = null;
        _userRepository
            .Setup(x => x.Add(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, _) => saved = user);

        var result = await _service.Register(new RegisterRequest("  newuser  ", "secret"));

        Assert.NotNull(saved);
        Assert.NotEqual(Guid.Empty, saved.Id);
        Assert.Equal("newuser", saved.Username);
        Assert.Equal("hashed:secret", saved.PasswordHash);
        Assert.Equal(Now.UtcDateTime, saved.CreatedAt);
        Assert.Equal(new UserDto(saved.Id, "newuser"), result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("ab")]
    public async Task Register_UsernameTooShort_IsRejected(string username)
    {
        var exception = await AssertRegisterIsRejected(new RegisterRequest(username, "secret"));

        Assert.Contains("username", exception.Errors.Keys);
    }

    [Fact]
    public async Task Register_UsernameTooLong_IsRejected()
    {
        var exception = await AssertRegisterIsRejected(new RegisterRequest(new string('a', UserService.MaxUsernameLength + 1), "secret"));

        Assert.Contains("username", exception.Errors.Keys);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1234")]
    public async Task Register_PasswordTooShort_IsRejected(string password)
    {
        var exception = await AssertRegisterIsRejected(new RegisterRequest("newuser", password));

        Assert.Contains("password", exception.Errors.Keys);
    }

    [Fact]
    public async Task Register_PasswordOfMinLength_IsAccepted()
    {
        await _service.Register(new RegisterRequest("newuser", "12345"));

        _userRepository.Verify(x => x.Add(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Register_TakenUsername_ThrowsConflict()
    {
        _userRepository.Setup(x => x.GetByUsername("admin", It.IsAny<CancellationToken>())).ReturnsAsync(ExistingUser());

        await Assert.ThrowsAsync<ConflictException>(() => _service.Register(new RegisterRequest("admin", "secret")));

        _userRepository.Verify(x => x.Add(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Authenticate_ValidCredentials_ReturnsToken()
    {
        var user = ExistingUser();
        var token = new AccessToken("jwt", Now.UtcDateTime.AddHours(1));
        _userRepository.Setup(x => x.GetByUsername("admin", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _tokenGenerator.Setup(x => x.Generate(user)).Returns(token);

        var result = await _service.Authenticate(new AuthenticateRequest(" admin ", "admin"));

        Assert.Equal(new AuthenticateResponse(user.Id, "admin", "jwt", token.ExpiresAt), result);
    }

    [Fact]
    public async Task Authenticate_WrongPassword_ReturnsNull()
    {
        _userRepository.Setup(x => x.GetByUsername("admin", It.IsAny<CancellationToken>())).ReturnsAsync(ExistingUser());

        var result = await _service.Authenticate(new AuthenticateRequest("admin", "wrong"));

        Assert.Null(result);
        _tokenGenerator.Verify(x => x.Generate(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Authenticate_UnknownUser_ReturnsNull()
    {
        var result = await _service.Authenticate(new AuthenticateRequest("nobody", "whatever"));

        Assert.Null(result);
        _tokenGenerator.Verify(x => x.Generate(It.IsAny<User>()), Times.Never);
    }

    [Theory]
    [InlineData("", "admin")]
    [InlineData("admin", "")]
    public async Task Authenticate_BlankCredentials_ReturnsNullWithoutLookingUp(string username, string password)
    {
        var result = await _service.Authenticate(new AuthenticateRequest(username, password));

        Assert.Null(result);
        _userRepository.Verify(x => x.GetByUsername(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetById_Existing_ReturnsIt()
    {
        var user = ExistingUser();
        _userRepository.Setup(x => x.GetById(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await _service.GetById(user.Id);

        Assert.Equal(new UserDto(user.Id, "admin"), result);
    }

    [Fact]
    public async Task GetById_Missing_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetById(Guid.NewGuid()));
    }
}
