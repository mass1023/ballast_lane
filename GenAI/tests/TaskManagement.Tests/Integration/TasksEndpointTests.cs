using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace TaskManagement.Tests.Integration;

public class TasksEndpointTests : IntegrationTest
{
    public TasksEndpointTests(ApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Create_Returns201WithLocationAndBody()
    {
        var response = await Client.PostAsJsonAsync("/api/tasks", NewTask("Ship it"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var task = await response.Content.ReadFromJsonAsync<TaskResponse>();
        Assert.Equal("Ship it", task!.Title);
        Assert.Equal("Pending", task.Status);
        Assert.Equal($"/api/tasks/{task.Id}", response.Headers.Location!.AbsolutePath);
    }

    [Fact]
    public async Task Create_WithInvalidFields_Returns400WithErrorPerField()
    {
        var response = await Client.PostAsJsonAsync("/api/tasks", new { title = "", dueDate = Today.AddDays(-1), responsableId = ApiFactory.Alice });

        var errors = await ValidationErrors(response);
        Assert.Contains("title", errors);
        Assert.Contains("dueDate", errors);
    }

    [Fact]
    public async Task Create_WithUnknownResponsable_Returns400()
    {
        var response = await Client.PostAsJsonAsync("/api/tasks", NewTask("Orphan", Guid.NewGuid()));

        Assert.Contains("responsableId", await ValidationErrors(response));
    }

    [Fact]
    public async Task Get_ReturnsTask()
    {
        var created = await Create("Read me");

        var task = await Client.GetFromJsonAsync<TaskResponse>($"/api/tasks/{created.Id}");

        Assert.Equal(created, task);
    }

    [Fact]
    public async Task Get_Unknown_Returns404ProblemDetails()
    {
        var response = await Client.GetAsync($"/api/tasks/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task List_FiltersByQueryString()
    {
        await Create("Task for Alice", ApiFactory.Alice);
        var bobs = await Create("Task for Bob", ApiFactory.Bob);
        await Client.PutAsJsonAsync($"/api/tasks/{bobs.Id}", Edit(bobs, status: "Done"));

        var all = await Client.GetFromJsonAsync<List<TaskResponse>>("/api/tasks");
        var alice = await Client.GetFromJsonAsync<List<TaskResponse>>($"/api/tasks?responsableId={ApiFactory.Alice}");
        var done = await Client.GetFromJsonAsync<List<TaskResponse>>("/api/tasks?status=Done");

        Assert.Equal(2, all!.Count);
        Assert.Equal("Task for Alice", Assert.Single(alice!).Title);
        Assert.Equal("Task for Bob", Assert.Single(done!).Title);
    }

    [Fact]
    public async Task List_WithInvalidStatus_Returns400()
    {
        var response = await Client.GetAsync("/api/tasks?status=Nope");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_ReplacesTask()
    {
        var created = await Create("Old");

        var response = await Client.PutAsJsonAsync($"/api/tasks/{created.Id}", Edit(created, title: "New", status: "InProgress"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var task = await Client.GetFromJsonAsync<TaskResponse>($"/api/tasks/{created.Id}");
        Assert.Equal("New", task!.Title);
        Assert.Equal("InProgress", task.Status);
    }

    [Fact]
    public async Task Update_WithUnknownStatus_Returns400()
    {
        var created = await Create("Task");

        var response = await Client.PutAsJsonAsync($"/api/tasks/{created.Id}", Edit(created, status: "Archived"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_Unknown_Returns404()
    {
        var created = await Create("Task");

        var response = await Client.PutAsJsonAsync($"/api/tasks/{Guid.NewGuid()}", Edit(created));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_HardDeletesTask()
    {
        var created = await Create("Delete me");

        var first = await Client.DeleteAsync($"/api/tasks/{created.Id}");
        var get = await Client.GetAsync($"/api/tasks/{created.Id}");
        var second = await Client.DeleteAsync($"/api/tasks/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, second.StatusCode);
    }

    private static object NewTask(string title, Guid? responsableId = null) =>
        new { title, description = "desc", dueDate = Today.AddDays(1), responsableId = responsableId ?? ApiFactory.Alice };

    private static object Edit(TaskResponse t, string? title = null, string? status = null) =>
        new { title = title ?? t.Title, t.Description, status = status ?? t.Status, t.DueDate, t.ResponsableId };

    private async Task<TaskResponse> Create(string title, Guid? responsableId = null)
    {
        var response = await Client.PostAsJsonAsync("/api/tasks", NewTask(title, responsableId));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TaskResponse>())!;
    }

    private static async Task<string[]> ValidationErrors(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("errors").EnumerateObject().Select(p => p.Name).ToArray();
    }

    private sealed record TaskResponse(Guid Id, string Title, string? Description, string Status, DateOnly DueDate, Guid ResponsableId);
}
