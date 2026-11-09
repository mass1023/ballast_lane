using Microsoft.Extensions.DependencyInjection;
using RecordLabel.Application.Abstractions;
using RecordLabel.Domain.Releases;

namespace RecordLabel.IntegrationTests.Persistence;

[Collection(PostgresCollection.Name)]
public sealed class ReleaseRepositoryTests(PostgresFixture fixture) : IAsyncLifetime
{
    private static readonly DateTime BaseTime = new(2026, 11, 9, 12, 0, 0, DateTimeKind.Utc);

    private AsyncServiceScope _scope;
    private IReleaseRepository _repository = null!;

    public async Task InitializeAsync()
    {
        await fixture.ResetDatabaseAsync();
        _scope = fixture.Services.CreateAsyncScope();
        _repository = _scope.ServiceProvider.GetRequiredService<IReleaseRepository>();
    }

    public async Task DisposeAsync() => await _scope.DisposeAsync();

    private static Release NewRelease(string title = "Kind of Blue", string artist = "Miles Davis", int minutesAfterBase = 0) => new()
    {
        Id = Guid.CreateVersion7(),
        Title = title,
        Artist = artist,
        ReleaseYear = 1959,
        Format = ReleaseFormat.Album,
        CreatedAt = BaseTime.AddMinutes(minutesAfterBase),
        UpdatedAt = BaseTime.AddMinutes(minutesAfterBase)
    };

    private async Task AddAll(params Release[] releases)
    {
        foreach (var release in releases)
            await _repository.Add(release, CancellationToken.None);
    }

    private static void AssertSameRelease(Release expected, Release? actual)
    {
        Assert.NotNull(actual);
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Title, actual.Title);
        Assert.Equal(expected.Artist, actual.Artist);
        Assert.Equal(expected.ReleaseYear, actual.ReleaseYear);
        Assert.Equal(expected.Format, actual.Format);
        Assert.Equal(expected.CreatedAt, actual.CreatedAt);
        Assert.Equal(expected.UpdatedAt, actual.UpdatedAt);
    }

    [Theory]
    [InlineData(ReleaseFormat.Single)]
    [InlineData(ReleaseFormat.EP)]
    [InlineData(ReleaseFormat.Album)]
    public async Task Add_ThenGetById_RoundTripsEveryField(ReleaseFormat format)
    {
        var release = NewRelease();
        release.Format = format;

        await _repository.Add(release, CancellationToken.None);

        AssertSameRelease(release, await _repository.GetById(release.Id, CancellationToken.None));
    }

    [Fact]
    public async Task GetById_Unknown_ReturnsNull()
    {
        Assert.Null(await _repository.GetById(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task Update_Existing_PersistsChangesAndReturnsTrue()
    {
        var release = NewRelease();
        await _repository.Add(release, CancellationToken.None);

        release.Title = "Sketches of Spain";
        release.ReleaseYear = 1960;
        release.Format = ReleaseFormat.EP;
        release.UpdatedAt = BaseTime.AddDays(1);
        var updated = await _repository.Update(release, CancellationToken.None);

        Assert.True(updated);
        AssertSameRelease(release, await _repository.GetById(release.Id, CancellationToken.None));
    }

    [Fact]
    public async Task Update_Missing_ReturnsFalse()
    {
        Assert.False(await _repository.Update(NewRelease(), CancellationToken.None));
    }

    [Fact]
    public async Task Delete_Existing_RemovesItAndReturnsTrue()
    {
        var release = NewRelease();
        await _repository.Add(release, CancellationToken.None);

        Assert.True(await _repository.Delete(release.Id, CancellationToken.None));
        Assert.Null(await _repository.GetById(release.Id, CancellationToken.None));
    }

    [Fact]
    public async Task Delete_Missing_ReturnsFalse()
    {
        Assert.False(await _repository.Delete(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task Search_WithoutTerm_ReturnsEverythingNewestFirst()
    {
        var oldest = NewRelease("A", minutesAfterBase: 0);
        var middle = NewRelease("B", minutesAfterBase: 1);
        var newest = NewRelease("C", minutesAfterBase: 2);
        await AddAll(oldest, newest, middle);

        var result = await _repository.Search(null, 1, 10, CancellationToken.None);

        Assert.Equal([newest.Id, middle.Id, oldest.Id], result.Items.Select(r => r.Id));
        Assert.Equal(3, result.TotalCount);
    }

    [Fact]
    public async Task Search_MatchesTitleOrArtistIgnoringCase()
    {
        var byTitle = NewRelease("Blue Train", "John Coltrane", 0);
        var byArtist = NewRelease("Some Album", "The Blues Brothers", 1);
        var noMatch = NewRelease("Thriller", "Michael Jackson", 2);
        await AddAll(byTitle, byArtist, noMatch);

        var result = await _repository.Search("BLUE", 1, 10, CancellationToken.None);

        Assert.Equal([byArtist.Id, byTitle.Id], result.Items.Select(r => r.Id));
        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task Search_TreatsLikeWildcardsAsPlainText()
    {
        await AddAll(NewRelease("100% Hits", minutesAfterBase: 0), NewRelease("Plain", minutesAfterBase: 1));

        var result = await _repository.Search("%", 1, 10, CancellationToken.None);

        Assert.Equal(["100% Hits"], result.Items.Select(r => r.Title));
    }

    [Fact]
    public async Task Search_ReturnsTheRequestedPageAndTheTotalCount()
    {
        var releases = Enumerable.Range(0, 5).Select(i => NewRelease($"Release {i}", minutesAfterBase: i)).ToArray();
        await AddAll(releases);

        var result = await _repository.Search(null, 2, 2, CancellationToken.None);

        Assert.Equal([releases[2].Id, releases[1].Id], result.Items.Select(r => r.Id));
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public async Task Search_PageAfterTheLast_ReturnsNoItemsButKeepsTheTotal()
    {
        await AddAll(NewRelease("Only one"));

        var result = await _repository.Search(null, 5, 10, CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(1, result.TotalCount);
    }
}
