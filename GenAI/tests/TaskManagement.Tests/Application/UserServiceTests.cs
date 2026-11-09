using Moq;
using TaskManagement.Application.Common;
using TaskManagement.Application.Users;
using TaskManagement.Domain.Users;

namespace TaskManagement.Tests.Application;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _sut = new UserService(_users.Object);
    }

    [Fact]
    public async Task ListAsync_MapsUsers()
    {
        var user = new User(Guid.NewGuid(), "alice", DateTimeOffset.UtcNow);
        _users.Setup(u => u.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync([user]);

        var result = await _sut.ListAsync();

        var dto = Assert.Single(result);
        Assert.Equal("alice", dto.Username);
    }

    [Fact]
    public async Task GetAsync_WhenMissing_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetAsync(Guid.NewGuid()));
    }
}
