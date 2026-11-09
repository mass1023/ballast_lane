using Moq;
using TaskManagement.Application.Common;
using TaskManagement.Application.Tasks;
using TaskManagement.Domain.Common;
using TaskManagement.Domain.Tasks;
using TaskManagement.Domain.Users;

namespace TaskManagement.Tests.Application;

public class TaskServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 9, 15, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now.UtcDateTime);
    private static readonly Guid UserId = Guid.NewGuid();

    private readonly Mock<ITaskRepository> _tasks = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly TaskService _sut;

    public TaskServiceTests()
    {
        _users.Setup(u => u.ExistsAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _sut = new TaskService(_tasks.Object, _users.Object, new FixedTimeProvider(Now));
    }

    [Fact]
    public async Task CreateAsync_PersistsTaskAndReturnsDto()
    {
        var request = new CreateTaskRequest("Write tests", "TDD", Today.AddDays(1), UserId);

        var result = await _sut.CreateAsync(request);

        Assert.Equal("Write tests", result.Title);
        Assert.Equal("Pending", result.Status);
        Assert.Equal(UserId, result.ResponsableId);
        _tasks.Verify(t => t.AddAsync(It.Is<TaskItem>(x => x.Id == result.Id), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_UsesTimeProviderForThePastDueDateRule()
    {
        var request = new CreateTaskRequest("Write tests", null, Today.AddDays(-1), UserId);

        var ex = await Assert.ThrowsAsync<DomainValidationException>(() => _sut.CreateAsync(request));

        Assert.Contains("dueDate", ex.Errors.Keys);
        _tasks.Verify(t => t.AddAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithUnknownResponsable_ThrowsValidation()
    {
        var request = new CreateTaskRequest("Write tests", null, Today, Guid.NewGuid());

        var ex = await Assert.ThrowsAsync<DomainValidationException>(() => _sut.CreateAsync(request));

        Assert.Contains("responsableId", ex.Errors.Keys);
        _tasks.Verify(t => t.AddAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithMissingResponsable_ThrowsValidation()
    {
        var request = new CreateTaskRequest("Write tests", null, Today, null);

        var ex = await Assert.ThrowsAsync<DomainValidationException>(() => _sut.CreateAsync(request));

        Assert.Contains("responsableId", ex.Errors.Keys);
    }

    [Fact]
    public async Task GetAsync_WhenMissing_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetAsync_ReturnsDto()
    {
        var task = Existing();

        var result = await _sut.GetAsync(task.Id);

        Assert.Equal(task.Id, result.Id);
        Assert.Equal(task.Title, result.Title);
    }

    [Fact]
    public async Task ListAsync_PassesFiltersToRepository()
    {
        var task = Existing();
        _tasks.Setup(t => t.ListAsync(TaskItemStatus.Pending, UserId, It.IsAny<CancellationToken>())).ReturnsAsync([task]);

        var result = await _sut.ListAsync(TaskItemStatus.Pending, UserId);

        Assert.Single(result);
    }

    [Fact]
    public async Task UpdateAsync_ChangesAndPersistsTask()
    {
        var task = Existing();
        var request = new UpdateTaskRequest("Renamed", "Desc", TaskItemStatus.InProgress, Today.AddDays(5), UserId);

        var result = await _sut.UpdateAsync(task.Id, request);

        Assert.Equal("Renamed", result.Title);
        Assert.Equal("InProgress", result.Status);
        _tasks.Verify(t => t.UpdateAsync(task, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenMissing_ThrowsNotFound()
    {
        var request = new UpdateTaskRequest("Renamed", null, TaskItemStatus.Done, Today, UserId);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.UpdateAsync(Guid.NewGuid(), request));
    }

    [Fact]
    public async Task UpdateAsync_WithUnknownResponsable_ThrowsValidation()
    {
        var task = Existing();
        var request = new UpdateTaskRequest("Renamed", null, TaskItemStatus.Done, Today, Guid.NewGuid());

        var ex = await Assert.ThrowsAsync<DomainValidationException>(() => _sut.UpdateAsync(task.Id, request));

        Assert.Contains("responsableId", ex.Errors.Keys);
        _tasks.Verify(t => t.UpdateAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithMissingStatus_ThrowsValidation()
    {
        var task = Existing();
        var request = new UpdateTaskRequest("Renamed", null, null, Today, UserId);

        var ex = await Assert.ThrowsAsync<DomainValidationException>(() => _sut.UpdateAsync(task.Id, request));

        Assert.Contains("status", ex.Errors.Keys);
    }

    [Fact]
    public async Task DeleteAsync_WhenRepositoryDeletes_Succeeds()
    {
        var id = Guid.NewGuid();
        _tasks.Setup(t => t.DeleteAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await _sut.DeleteAsync(id);

        _tasks.Verify(t => t.DeleteAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenNothingDeleted_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(Guid.NewGuid()));
    }

    private TaskItem Existing()
    {
        var task = TaskItem.Create("Existing", null, Today.AddDays(2), UserId, Today);
        _tasks.Setup(t => t.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        return task;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
