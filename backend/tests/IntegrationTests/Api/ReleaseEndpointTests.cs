using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using RecordLabel.Application.Common;
using RecordLabel.Application.Releases;
using RecordLabel.Domain.Releases;

namespace RecordLabel.IntegrationTests.Api;

public sealed class ReleaseEndpointTests(PostgresFixture fixture) : ApiTestBase(fixture)
{
    private static object Body(string title = "Kind of Blue", string artist = "Miles Davis", int releaseYear = 1959, string format = "Album") =>
        new { title, artist, releaseYear, format };

    private async Task<ReleaseDto> Create(object body)
    {
        var response = await Client.PostAsJsonAsync("/api/releases", body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ReleaseDto>(Json))!;
    }

    public static TheoryData<string, string> AllEndpoints => new()
    {
        { "GET", "/api/releases" },
        { "GET", $"/api/releases/{Guid.NewGuid()}" },
        { "POST", "/api/releases" },
        { "PUT", $"/api/releases/{Guid.NewGuid()}" },
        { "DELETE", $"/api/releases/{Guid.NewGuid()}" }
    };

    [Theory]
    [MemberData(nameof(AllEndpoints))]
    public async Task Endpoint_WithoutToken_Returns401(string method, string url)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (method is "POST" or "PUT")
            request.Content = JsonContent.Create(Body());

        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_ValidRelease_Returns201WithLocationAndTheRelease()
    {
        await LoginAsAdmin();

        var response = await Client.PostAsJsonAsync("/api/releases", Body(title: "  Blue Train ", artist: "John Coltrane", releaseYear: 1957));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ReleaseDto>(Json);
        Assert.Equal("Blue Train", created!.Title);
        Assert.Equal(ReleaseFormat.Album, created.Format);
        Assert.Equal($"/api/releases/{created.Id}", response.Headers.Location!.AbsolutePath, ignoreCase: true);

        var fetched = await Client.GetFromJsonAsync<ReleaseDto>(response.Headers.Location, Json);
        Assert.Equal(created, fetched);
    }

    [Fact]
    public async Task Create_SendsFormatAsText()
    {
        await LoginAsAdmin();
        var created = await Create(Body(format: "EP"));

        var raw = await Client.GetStringAsync($"/api/releases/{created.Id}");

        Assert.Contains("\"format\":\"EP\"", raw);
    }

    [Fact]
    public async Task Create_BrokenRules_Returns400WithEveryFieldError()
    {
        await LoginAsAdmin();

        var response = await Client.PostAsJsonAsync("/api/releases", Body(title: "", artist: "", releaseYear: 1800));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(Json);
        Assert.Contains("title", problem!.Errors.Keys);
        Assert.Contains("artist", problem.Errors.Keys);
        Assert.Contains("releaseYear", problem.Errors.Keys);
    }

    [Fact]
    public async Task Create_UnknownFormat_Returns400()
    {
        await LoginAsAdmin();

        var response = await Client.PostAsJsonAsync("/api/releases", Body(format: "Cassette"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_Existing_Returns200WithTheChanges()
    {
        await LoginAsAdmin();
        var created = await Create(Body());

        var response = await Client.PutAsJsonAsync($"/api/releases/{created.Id}", Body(title: "Sketches of Spain", releaseYear: 1960));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await Client.GetFromJsonAsync<ReleaseDto>($"/api/releases/{created.Id}", Json);
        Assert.Equal("Sketches of Spain", updated!.Title);
        Assert.Equal(1960, updated.ReleaseYear);
        Assert.Equal(created.CreatedAt, updated.CreatedAt);
    }

    [Fact]
    public async Task Update_Missing_Returns404()
    {
        await LoginAsAdmin();

        var response = await Client.PutAsJsonAsync($"/api/releases/{Guid.NewGuid()}", Body());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Existing_Returns204AndTheReleaseIsGone()
    {
        await LoginAsAdmin();
        var created = await Create(Body());

        var delete = await Client.DeleteAsync($"/api/releases/{created.Id}");
        var get = await Client.GetAsync($"/api/releases/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task Delete_Missing_Returns404()
    {
        await LoginAsAdmin();

        var response = await Client.DeleteAsync($"/api/releases/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task List_SearchesAndPaginates()
    {
        await LoginAsAdmin();
        await Create(Body(title: "Zebrafish One"));
        await Create(Body(title: "Zebrafish Two"));
        await Create(Body(artist: "The Zebrafish Band"));

        var page = await Client.GetFromJsonAsync<PagedResult<ReleaseDto>>("/api/releases?search=zebrafish&page=1&pageSize=2", Json);

        Assert.Equal(3, page!.TotalCount);
        Assert.Equal(2, page.Items.Count);
        Assert.Equal(2, page.TotalPages);
    }

    [Fact]
    public async Task List_WithoutPagingParameters_UsesTheFirstPage()
    {
        await LoginAsAdmin();

        var page = await Client.GetFromJsonAsync<PagedResult<ReleaseDto>>("/api/releases", Json);

        Assert.Equal(1, page!.Page);
        Assert.Equal(10, page.PageSize);
    }

    [Fact]
    public async Task List_PageSizeTooBig_Returns400()
    {
        await LoginAsAdmin();

        var response = await Client.GetAsync("/api/releases?pageSize=1000");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
