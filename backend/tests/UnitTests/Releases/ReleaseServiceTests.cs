using Moq;
using RecordLabel.Application.Abstractions;
using RecordLabel.Application.Common;
using RecordLabel.Application.Releases;
using RecordLabel.Domain.Releases;

namespace RecordLabel.UnitTests.Releases;

public class ReleaseServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 9, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<IReleaseRepository> _releaseRepository = new();
    private readonly Mock<TimeProvider> _timeProvider = new();
    private readonly ReleaseService _service;

    public ReleaseServiceTests()
    {
        _timeProvider.Setup(x => x.GetUtcNow()).Returns(Now);
        _service = new ReleaseService(_releaseRepository.Object, _timeProvider.Object);
    }

    private static SaveReleaseRequest ValidRequest() =>
        new("Kind of Blue", "Miles Davis", 1959, ReleaseFormat.Album);

    private static Release ExistingRelease() => new()
    {
        Id = Guid.NewGuid(),
        Title = "Old title",
        Artist = "Old artist",
        ReleaseYear = 2000,
        Format = ReleaseFormat.Single,
        CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    private async Task<ValidationException> AssertCreateIsRejected(SaveReleaseRequest request)
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(() => _service.Create(request));
        _releaseRepository.Verify(x => x.Add(It.IsAny<Release>(), It.IsAny<CancellationToken>()), Times.Never);
        return exception;
    }

    [Fact]
    public async Task Create_ValidRequest_SavesTrimmedReleaseAndReturnsIt()
    {
        Release? saved = null;
        _releaseRepository
            .Setup(x => x.Add(It.IsAny<Release>(), It.IsAny<CancellationToken>()))
            .Callback<Release, CancellationToken>((release, _) => saved = release);

        var result = await _service.Create(new SaveReleaseRequest("  Kind of Blue  ", " Miles Davis ", 1959, ReleaseFormat.Album));

        Assert.NotNull(saved);
        Assert.NotEqual(Guid.Empty, saved.Id);
        Assert.Equal("Kind of Blue", saved.Title);
        Assert.Equal("Miles Davis", saved.Artist);
        Assert.Equal(1959, saved.ReleaseYear);
        Assert.Equal(ReleaseFormat.Album, saved.Format);
        Assert.Equal(Now.UtcDateTime, saved.CreatedAt);
        Assert.Equal(Now.UtcDateTime, saved.UpdatedAt);
        Assert.Equal(saved.ToDto(), result);
    }

    [Fact]
    public async Task Create_TruncatesTimestampsToMicroseconds()
    {
        _timeProvider.Setup(x => x.GetUtcNow()).Returns(Now.AddTicks(1234567));

        var result = await _service.Create(ValidRequest());

        Assert.Equal(Now.UtcDateTime.AddTicks(1234560), result.CreatedAt);
        Assert.Equal(result.CreatedAt, result.UpdatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_BlankTitle_IsRejected(string title)
    {
        var exception = await AssertCreateIsRejected(ValidRequest() with { Title = title });

        Assert.Contains("title", exception.Errors.Keys);
    }

    [Fact]
    public async Task Create_TitleLongerThanMax_IsRejected()
    {
        var exception = await AssertCreateIsRejected(ValidRequest() with { Title = new string('a', ReleaseService.MaxTextLength + 1) });

        Assert.Contains("title", exception.Errors.Keys);
    }

    [Fact]
    public async Task Create_TitleOfMaxLength_IsAccepted()
    {
        await _service.Create(ValidRequest() with { Title = new string('a', ReleaseService.MaxTextLength) });

        _releaseRepository.Verify(x => x.Add(It.IsAny<Release>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_BlankArtist_IsRejected()
    {
        var exception = await AssertCreateIsRejected(ValidRequest() with { Artist = " " });

        Assert.Contains("artist", exception.Errors.Keys);
    }

    [Fact]
    public async Task Create_ArtistLongerThanMax_IsRejected()
    {
        var exception = await AssertCreateIsRejected(ValidRequest() with { Artist = new string('a', ReleaseService.MaxTextLength + 1) });

        Assert.Contains("artist", exception.Errors.Keys);
    }

    [Theory]
    [InlineData(1899)]
    [InlineData(2028)]
    public async Task Create_YearOutOfRange_IsRejected(int year)
    {
        var exception = await AssertCreateIsRejected(ValidRequest() with { ReleaseYear = year });

        Assert.Contains("releaseYear", exception.Errors.Keys);
    }

    [Theory]
    [InlineData(1900)]
    [InlineData(2027)]
    public async Task Create_YearOnRangeBoundary_IsAccepted(int year)
    {
        await _service.Create(ValidRequest() with { ReleaseYear = year });

        _releaseRepository.Verify(x => x.Add(It.IsAny<Release>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_UnknownFormat_IsRejected()
    {
        var exception = await AssertCreateIsRejected(ValidRequest() with { Format = (ReleaseFormat)99 });

        Assert.Contains("format", exception.Errors.Keys);
    }

    [Fact]
    public async Task Create_SeveralInvalidFields_ReportsAllOfThem()
    {
        var exception = await AssertCreateIsRejected(new SaveReleaseRequest("", "", 1800, (ReleaseFormat)0));

        Assert.Equal(["artist", "format", "releaseYear", "title"], exception.Errors.Keys.Order());
    }

    [Fact]
    public async Task GetById_Existing_ReturnsIt()
    {
        var existing = ExistingRelease();
        _releaseRepository.Setup(x => x.GetById(existing.Id, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var result = await _service.GetById(existing.Id);

        Assert.Equal(existing.ToDto(), result);
    }

    [Fact]
    public async Task GetById_Missing_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetById(Guid.NewGuid()));
    }

    [Fact]
    public async Task Update_Existing_ChangesFieldsAndKeepsCreatedAt()
    {
        var existing = ExistingRelease();
        var createdAt = existing.CreatedAt;
        _releaseRepository.Setup(x => x.GetById(existing.Id, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _releaseRepository.Setup(x => x.Update(It.IsAny<Release>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _service.Update(existing.Id, new SaveReleaseRequest(" Blue Train ", "John Coltrane", 1957, ReleaseFormat.Album));

        _releaseRepository.Verify(x => x.Update(It.Is<Release>(r =>
            r.Id == existing.Id &&
            r.Title == "Blue Train" &&
            r.Artist == "John Coltrane" &&
            r.ReleaseYear == 1957 &&
            r.Format == ReleaseFormat.Album &&
            r.CreatedAt == createdAt &&
            r.UpdatedAt == Now.UtcDateTime), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal("Blue Train", result.Title);
    }

    [Fact]
    public async Task Update_Missing_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.Update(Guid.NewGuid(), ValidRequest()));

        _releaseRepository.Verify(x => x.Update(It.IsAny<Release>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_DeletedWhileEditing_ThrowsNotFound()
    {
        var existing = ExistingRelease();
        _releaseRepository.Setup(x => x.GetById(existing.Id, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _releaseRepository.Setup(x => x.Update(It.IsAny<Release>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.Update(existing.Id, ValidRequest()));
    }

    [Fact]
    public async Task Update_InvalidRequest_IsRejectedBeforeTouchingTheRepository()
    {
        await Assert.ThrowsAsync<ValidationException>(() => _service.Update(Guid.NewGuid(), ValidRequest() with { Title = "" }));

        _releaseRepository.Verify(x => x.GetById(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Delete_Existing_DeletesIt()
    {
        var id = Guid.NewGuid();
        _releaseRepository.Setup(x => x.Delete(id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await _service.Delete(id);

        _releaseRepository.Verify(x => x.Delete(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_Missing_ThrowsNotFound()
    {
        _releaseRepository.Setup(x => x.Delete(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.Delete(Guid.NewGuid()));
    }

    [Theory]
    [InlineData("  miles  ", "miles")]
    [InlineData("   ", null)]
    [InlineData(null, null)]
    public async Task Search_TrimsTheSearchTermAndMapsResults(string? search, string? expectedSearch)
    {
        var release = ExistingRelease();
        _releaseRepository
            .Setup(x => x.Search(expectedSearch, 2, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Release>([release], 11, 2, 10));

        var result = await _service.Search(search, 2, 10);

        Assert.Equal([release.ToDto()], result.Items);
        Assert.Equal(11, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, ReleaseService.MaxPageSize + 1)]
    public async Task Search_InvalidPaging_IsRejected(int page, int pageSize)
    {
        await Assert.ThrowsAsync<ValidationException>(() => _service.Search(null, page, pageSize));
    }
}
