using Microsoft.Extensions.DependencyInjection;
using TaskManagement.Domain.Tasks;

namespace TaskManagement.Tests.Integration;

public class TaskRepositoryTests : IntegrationTest
{
    public TaskRepositoryTests(ApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task AddThenGet_RoundTripsEveryField()
    {
        using var scope = Factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ITaskRepository>();
        var task = TaskItem.Create("Round trip", "All fields", Today.AddDays(4), ApiFactory.Alice, Today);

        await repo.AddAsync(task);
        var loaded = await repo.GetByIdAsync(task.Id);

        Assert.NotNull(loaded);
        Assert.Equal(task.Title, loaded.Title);
        Assert.Equal(task.Description, loaded.Description);
        Assert.Equal(task.Status, loaded.Status);
        Assert.Equal(task.DueDate, loaded.DueDate);
        Assert.Equal(task.ResponsableId, loaded.ResponsableId);
    }

    [Fact]
    public async Task Update_PersistsChanges()
    {
        using var scope = Factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ITaskRepository>();
        var task = TaskItem.Create("Before", null, Today, ApiFactory.Alice, Today);
        await repo.AddAsync(task);

        task.Update("After", "now with description", TaskItemStatus.Done, Today.AddDays(1), ApiFactory.Bob);
        await repo.UpdateAsync(task);
        var loaded = await repo.GetByIdAsync(task.Id);

        Assert.Equal("After", loaded!.Title);
        Assert.Equal("now with description", loaded.Description);
        Assert.Equal(TaskItemStatus.Done, loaded.Status);
        Assert.Equal(ApiFactory.Bob, loaded.ResponsableId);
    }

    [Fact]
    public async Task List_FiltersByStatusAndResponsable()
    {
        using var scope = Factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ITaskRepository>();
        var a = TaskItem.Create("A", null, Today, ApiFactory.Alice, Today);
        var b = TaskItem.Create("B", null, Today, ApiFactory.Bob, Today);
        var c = TaskItem.Create("C", null, Today, ApiFactory.Alice, Today);
        c.Update("C", null, TaskItemStatus.Done, Today, ApiFactory.Alice);
        await repo.AddAsync(a);
        await repo.AddAsync(b);
        await repo.AddAsync(c);

        Assert.Equal(3, (await repo.ListAsync(null, null)).Count);
        Assert.Equal(2, (await repo.ListAsync(null, ApiFactory.Alice)).Count);
        Assert.Equal("C", Assert.Single(await repo.ListAsync(TaskItemStatus.Done, null)).Title);
        Assert.Equal("A", Assert.Single(await repo.ListAsync(TaskItemStatus.Pending, ApiFactory.Alice)).Title);
    }

    [Fact]
    public async Task Delete_RemovesRowAndReportsWhetherItExisted()
    {
        using var scope = Factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ITaskRepository>();
        var task = TaskItem.Create("Doomed", null, Today, ApiFactory.Alice, Today);
        await repo.AddAsync(task);

        Assert.True(await repo.DeleteAsync(task.Id));
        Assert.Null(await repo.GetByIdAsync(task.Id));
        Assert.False(await repo.DeleteAsync(task.Id));
    }
}
