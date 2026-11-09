using RecordLabel.Application.Abstractions;
using RecordLabel.Application.Common;
using RecordLabel.Domain.Releases;

namespace RecordLabel.Application.Releases;

public interface IReleaseService
{
    Task<PagedResult<ReleaseDto>> Search(string? search, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ReleaseDto> GetById(Guid id, CancellationToken cancellationToken = default);
    Task<ReleaseDto> Create(SaveReleaseRequest request, CancellationToken cancellationToken = default);
    Task<ReleaseDto> Update(Guid id, SaveReleaseRequest request, CancellationToken cancellationToken = default);
    Task Delete(Guid id, CancellationToken cancellationToken = default);
}

public class ReleaseService : IReleaseService
{
    public const int MinReleaseYear = 1900;
    public const int MaxTextLength = 200;
    public const int MaxPageSize = 50;

    private readonly IReleaseRepository _releases;
    private readonly TimeProvider _clock;

    public ReleaseService(IReleaseRepository releaseRepository, TimeProvider timeProvider)
    {
        _clock = timeProvider;
        _releases = releaseRepository;
    }

    public async Task<PagedResult<ReleaseDto>> Search(string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var errors = new Dictionary<string, string[]>();
        if (pageSize < 1 || pageSize > MaxPageSize)
            errors["pageSize"] = [$"Page size must be between 1 and {MaxPageSize}."];
        if (page < 1)
            errors["page"] = ["Page must be 1 or greater."];
        if (errors.Count > 0)
            throw new ValidationException(errors);

        var term = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        var result = await _releases.Search(term, page, pageSize, cancellationToken);
        return result.Map(release => release.ToDto());
    }

    public async Task<ReleaseDto> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var release = await _releases.GetById(id, cancellationToken);
        if (release is null)
            throw new NotFoundException($"Release {id} was not found.");
        return release.ToDto();
    }

    public async Task<ReleaseDto> Create(SaveReleaseRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);

        var now = _clock.UtcNowInMicroseconds();
        var release = new Release
        {
            Id = Guid.CreateVersion7(),
            CreatedAt = now,
            UpdatedAt = now
        };
        release.Apply(request);

        await _releases.Add(release, cancellationToken);
        return release.ToDto();
    }

    public async Task<ReleaseDto> Update(Guid id, SaveReleaseRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);

        var release = await _releases.GetById(id, cancellationToken);
        if (release is null)
            throw new NotFoundException($"Release {id} was not found.");
        release.Apply(request);
        release.UpdatedAt = _clock.UtcNowInMicroseconds();

        if (!await _releases.Update(release, cancellationToken))
            throw new NotFoundException($"Release {id} was not found.");

        return release.ToDto();
    }

    public async Task Delete(Guid id, CancellationToken cancellationToken = default)
    {
        if (!await _releases.Delete(id, cancellationToken))
            throw new NotFoundException($"Release {id} was not found.");
    }

    private void Validate(SaveReleaseRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        var maxYear = _clock.GetUtcNow().Year + 1;

        if (!Enum.IsDefined(request.Format))
            errors["format"] = ["Format must be Single, EP or Album."];

        if (string.IsNullOrWhiteSpace(request.Title))
            errors["title"] = ["Title is required."];
        else if (request.Title.Trim().Length > MaxTextLength)
            errors["title"] = [$"Title can't be longer than {MaxTextLength} characters."];

        if (string.IsNullOrWhiteSpace(request.Artist))
            errors["artist"] = ["Artist is required."];
        else if (request.Artist.Trim().Length > MaxTextLength)
            errors["artist"] = [$"Artist can't be longer than {MaxTextLength} characters."];

        if (request.ReleaseYear < MinReleaseYear || request.ReleaseYear > maxYear)
            errors["releaseYear"] = [$"Release year must be between {MinReleaseYear} and {maxYear}."];

        if (errors.Count > 0)
            throw new ValidationException(errors);
    }
}
