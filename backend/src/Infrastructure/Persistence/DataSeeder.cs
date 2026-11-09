using RecordLabel.Application.Abstractions;
using RecordLabel.Application.Common;
using RecordLabel.Domain.Releases;
using RecordLabel.Domain.Users;

namespace RecordLabel.Infrastructure.Persistence;

public class DataSeeder
{
    public const string AdminUsername = "admin";
    public const string AdminPassword = "admin";

    private static readonly (string Title, string Artist, int Year, ReleaseFormat Format)[] DemoReleases =
    [
        ("Blue Train", "John Coltrane", 1957, ReleaseFormat.Album),
        ("Kind of Blue", "Miles Davis", 1959, ReleaseFormat.Album),
        ("Abbey Road", "The Beatles", 1969, ReleaseFormat.Album),
        ("Rumours", "Fleetwood Mac", 1977, ReleaseFormat.Album),
        ("Thriller", "Michael Jackson", 1982, ReleaseFormat.Album),
        ("Billie Jean", "Michael Jackson", 1983, ReleaseFormat.Single),
        ("Nevermind", "Nirvana", 1991, ReleaseFormat.Album),
        ("Smells Like Teen Spirit", "Nirvana", 1991, ReleaseFormat.Single),
        ("Hormoaning", "Nirvana", 1992, ReleaseFormat.EP),
        ("My Iron Lung", "Radiohead", 1994, ReleaseFormat.EP),
        ("OK Computer", "Radiohead", 1997, ReleaseFormat.Album),
        ("Homogenic", "Björk", 1997, ReleaseFormat.Album),
        ("Random Access Memories", "Daft Punk", 2013, ReleaseFormat.Album),
        ("Get Lucky", "Daft Punk", 2013, ReleaseFormat.Single)
    ];

    private readonly IUserRepository _users;
    private readonly IReleaseRepository _releases;
    private readonly IPasswordHasher _hasher;
    private readonly TimeProvider _clock;

    public DataSeeder(IUserRepository userRepository, IReleaseRepository releaseRepository, IPasswordHasher passwordHasher, TimeProvider timeProvider)
    {
        _users = userRepository;
        _releases = releaseRepository;
        _hasher = passwordHasher;
        _clock = timeProvider;
    }

    public async Task Seed(CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNowInMicroseconds();

        if (await _users.GetByUsername(AdminUsername, cancellationToken) == null)
        {
            await _users.Add(new User
            {
                Id = Guid.CreateVersion7(),
                Username = AdminUsername,
                PasswordHash = _hasher.Hash(AdminPassword),
                CreatedAt = now
            }, cancellationToken);
        }

        var catalog = await _releases.Search(null, 1, 1, cancellationToken);
        if (catalog.TotalCount > 0)
            return;

        for (var i = 0; i < DemoReleases.Length; i++)
        {
            var (title, artist, year, format) = DemoReleases[i];
            var createdAt = now.AddSeconds(i - DemoReleases.Length);
            await _releases.Add(new Release
            {
                Id = Guid.CreateVersion7(),
                Title = title,
                Artist = artist,
                ReleaseYear = year,
                Format = format,
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            }, cancellationToken);
        }
    }
}
