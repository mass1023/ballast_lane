using Microsoft.Extensions.DependencyInjection;
using RecordLabel.Application.Abstractions;
using RecordLabel.Application.Common;
using RecordLabel.Domain.Users;

namespace RecordLabel.IntegrationTests.Persistence;

[Collection(PostgresCollection.Name)]
public sealed class UserRepositoryTests(PostgresFixture fixture) : IAsyncLifetime
{
    private AsyncServiceScope _scope;
    private IUserRepository _repository = null!;

    public async Task InitializeAsync()
    {
        await fixture.ResetDatabaseAsync();
        _scope = fixture.Services.CreateAsyncScope();
        _repository = _scope.ServiceProvider.GetRequiredService<IUserRepository>();
    }

    public async Task DisposeAsync() => await _scope.DisposeAsync();

    private static User NewUser(string username) => new()
    {
        Id = Guid.CreateVersion7(),
        Username = username,
        PasswordHash = "hash",
        CreatedAt = new DateTime(2026, 11, 9, 12, 0, 0, DateTimeKind.Utc)
    };

    private static void AssertSameUser(User expected, User? actual)
    {
        Assert.NotNull(actual);
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Username, actual.Username);
        Assert.Equal(expected.PasswordHash, actual.PasswordHash);
        Assert.Equal(expected.CreatedAt, actual.CreatedAt);
    }

    [Fact]
    public async Task Add_ThenGetById_ReturnsTheUser()
    {
        var user = NewUser("Adrian");
        await _repository.Add(user, CancellationToken.None);

        AssertSameUser(user, await _repository.GetById(user.Id, CancellationToken.None));
    }

    [Fact]
    public async Task GetByUsername_IgnoresCase()
    {
        var user = NewUser("Adrian");
        await _repository.Add(user, CancellationToken.None);

        AssertSameUser(user, await _repository.GetByUsername("aDRIAN", CancellationToken.None));
    }

    [Fact]
    public async Task GetByUsername_Unknown_ReturnsNull()
    {
        Assert.Null(await _repository.GetByUsername("nobody", CancellationToken.None));
    }

    [Fact]
    public async Task GetById_Unknown_ReturnsNull()
    {
        Assert.Null(await _repository.GetById(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task Add_SameUsernameWithDifferentCase_ThrowsConflict()
    {
        await _repository.Add(NewUser("adrian"), CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() => _repository.Add(NewUser("ADRIAN"), CancellationToken.None));
    }
}
