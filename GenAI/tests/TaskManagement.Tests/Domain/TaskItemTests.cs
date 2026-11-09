using TaskManagement.Domain.Common;
using TaskManagement.Domain.Tasks;

namespace TaskManagement.Tests.Domain;

public class TaskItemTests
{
    private static readonly DateOnly Today = new(2026, 11, 9);
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_StartsAsPending()
    {
        var task = TaskItem.Create("Write tests", "TDD first", Today.AddDays(3), UserId, Today);

        Assert.NotEqual(Guid.Empty, task.Id);
        Assert.Equal("Write tests", task.Title);
        Assert.Equal("TDD first", task.Description);
        Assert.Equal(TaskItemStatus.Pending, task.Status);
        Assert.Equal(Today.AddDays(3), task.DueDate);
        Assert.Equal(UserId, task.ResponsableId);
    }

    [Fact]
    public void Create_TrimsTitleAndTurnsBlankDescriptionIntoNull()
    {
        var task = TaskItem.Create("  Write tests  ", "   ", Today, UserId, Today);

        Assert.Equal("Write tests", task.Title);
        Assert.Null(task.Description);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithoutTitle_Throws(string? title)
    {
        var ex = Assert.Throws<DomainValidationException>(() => TaskItem.Create(title, null, Today, UserId, Today));

        Assert.Contains("title", ex.Errors.Keys);
    }

    [Fact]
    public void Create_WithTitleOver200Chars_Throws()
    {
        var ex = Assert.Throws<DomainValidationException>(() => TaskItem.Create(new string('a', 201), null, Today, UserId, Today));

        Assert.Contains("title", ex.Errors.Keys);
    }

    [Fact]
    public void Create_WithDescriptionOver2000Chars_Throws()
    {
        var ex = Assert.Throws<DomainValidationException>(() => TaskItem.Create("Title", new string('a', 2001), Today, UserId, Today));

        Assert.Contains("description", ex.Errors.Keys);
    }

    [Fact]
    public void Create_WithoutDueDate_Throws()
    {
        var ex = Assert.Throws<DomainValidationException>(() => TaskItem.Create("Title", null, null, UserId, Today));

        Assert.Contains("dueDate", ex.Errors.Keys);
    }

    [Fact]
    public void Create_WithDueDateInThePast_Throws()
    {
        var ex = Assert.Throws<DomainValidationException>(() => TaskItem.Create("Title", null, Today.AddDays(-1), UserId, Today));

        Assert.Contains("dueDate", ex.Errors.Keys);
    }

    [Fact]
    public void Create_WithoutResponsable_Throws()
    {
        var ex = Assert.Throws<DomainValidationException>(() => TaskItem.Create("Title", null, Today, Guid.Empty, Today));

        Assert.Contains("responsableId", ex.Errors.Keys);
    }

    [Fact]
    public void Create_CollectsEveryError()
    {
        var ex = Assert.Throws<DomainValidationException>(() => TaskItem.Create("", null, null, Guid.Empty, Today));

        Assert.Equal(3, ex.Errors.Count);
    }

    [Fact]
    public void Update_ReplacesAllFields()
    {
        var task = TaskItem.Create("Title", null, Today, UserId, Today);
        var other = Guid.NewGuid();

        task.Update("New title", "New description", TaskItemStatus.Done, Today.AddDays(10), other);

        Assert.Equal("New title", task.Title);
        Assert.Equal("New description", task.Description);
        Assert.Equal(TaskItemStatus.Done, task.Status);
        Assert.Equal(Today.AddDays(10), task.DueDate);
        Assert.Equal(other, task.ResponsableId);
    }

    [Fact]
    public void Update_AllowsDueDateInThePast()
    {
        var task = TaskItem.Load(Guid.NewGuid(), "Old task", null, TaskItemStatus.InProgress, Today.AddDays(-30), UserId);

        task.Update("Old task", null, TaskItemStatus.Done, Today.AddDays(-30), UserId);

        Assert.Equal(TaskItemStatus.Done, task.Status);
    }

    [Fact]
    public void Update_WithInvalidData_ThrowsAndLeavesTaskUnchanged()
    {
        var task = TaskItem.Create("Title", null, Today, UserId, Today);

        Assert.Throws<DomainValidationException>(() => task.Update("", null, TaskItemStatus.Done, null, UserId));

        Assert.Equal("Title", task.Title);
        Assert.Equal(TaskItemStatus.Pending, task.Status);
    }

    [Fact]
    public void Update_WithUndefinedStatus_Throws()
    {
        var task = TaskItem.Create("Title", null, Today, UserId, Today);

        var ex = Assert.Throws<DomainValidationException>(() => task.Update("Title", null, (TaskItemStatus)99, Today, UserId));

        Assert.Contains("status", ex.Errors.Keys);
    }
}
