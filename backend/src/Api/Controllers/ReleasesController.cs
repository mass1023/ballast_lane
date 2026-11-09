using Microsoft.AspNetCore.Mvc;
using RecordLabel.Application.Common;
using RecordLabel.Application.Releases;

namespace RecordLabel.Api.Controllers;

public class ReleasesController : ApiBase
{
    private readonly IReleaseService _releases;

    public ReleasesController(IReleaseService releaseService)
    {
        _releases = releaseService;
    }

    [HttpGet]
    [ProducesResponseType<PagedResult<ReleaseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search([FromQuery] string? search, CancellationToken cancellationToken, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        return Ok(await _releases.Search(search, page, pageSize, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ReleaseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _releases.GetById(id, cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType<ReleaseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] SaveReleaseRequest request, CancellationToken cancellationToken)
    {
        var release = await _releases.Create(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = release.Id }, release);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<ReleaseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] SaveReleaseRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _releases.Update(id, request, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _releases.Delete(id, cancellationToken);
        return NoContent();
    }
}
