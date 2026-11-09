using RecordLabel.Domain.Releases;

namespace RecordLabel.Application.Releases;

public record SaveReleaseRequest(
    string Title,
    string Artist,
    int ReleaseYear,
    ReleaseFormat Format);

public record ReleaseDto(
    Guid Id,
    string Title,
    string Artist,
    int ReleaseYear,
    ReleaseFormat Format,
    DateTime CreatedAt,
    DateTime UpdatedAt);
