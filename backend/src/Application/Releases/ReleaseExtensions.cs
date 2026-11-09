using RecordLabel.Domain.Releases;

namespace RecordLabel.Application.Releases;

public static class ReleaseExtensions
{
    public static ReleaseDto ToDto(this Release release)
    {
        return new ReleaseDto(
            release.Id,
            release.Title,
            release.Artist,
            release.ReleaseYear,
            release.Format,
            release.CreatedAt,
            release.UpdatedAt);
    }

    public static void Apply(this Release release, SaveReleaseRequest request)
    {
        release.Title = request.Title.Trim();
        release.Artist = request.Artist.Trim();
        release.ReleaseYear = request.ReleaseYear;
        release.Format = request.Format;
    }
}
