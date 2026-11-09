using RecordLabel.Application.Common;
using RecordLabel.Domain.Releases;

namespace RecordLabel.Application.Abstractions;

public interface IReleaseRepository
{
    Task<PagedResult<Release>> Search(string? search, int page, int pageSize, CancellationToken cancellationToken);
    Task<Release?> GetById(Guid id, CancellationToken cancellationToken);
    Task Add(Release release, CancellationToken cancellationToken);
    Task<bool> Update(Release release, CancellationToken cancellationToken);
    Task<bool> Delete(Guid id, CancellationToken cancellationToken);
}
