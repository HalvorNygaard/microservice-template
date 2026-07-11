using MicroserviceTemplate.Features.Tasks;
using Microsoft.Extensions.Time.Testing;

namespace MicroserviceTemplate.UnitTests.Tasks;

public sealed class TaskItemTests
{
    private static readonly DateTimeOffset Start = new(2026, 7, 11, 8, 0, 0, TimeSpan.Zero);

    [Test]
    public void Create_Uses_Version7_Identifiers_And_Injected_Time()
    {
        FakeTimeProvider timeProvider = new(Start);
        DateTimeOffset dueDate = Start.ToOffset(TimeSpan.FromHours(2)).AddDays(1);

        TaskItem task = TaskItem.Create("  Write tests  ", "  Verify domain behavior clearly.  ", dueDate, timeProvider);

        task.Id.ToString("N")[12].ShouldBe('7');
        task.Version.ToString("N")[12].ShouldBe('7');
        task.Title.ShouldBe("Write tests");
        task.Description.ShouldBe("Verify domain behavior clearly.");
        task.DueDate.ShouldBe(dueDate.ToUniversalTime());
        task.CreatedAt.ShouldBe(Start);
        task.UpdatedAt.ShouldBe(Start);
        task.Status.ShouldBe(TaskItemStatus.Todo);
    }

    [Test]
    public void Update_Advances_Timestamp_And_Concurrency_Version()
    {
        FakeTimeProvider timeProvider = new(Start);
        TaskItem task = TaskItem.Create("Initial title", "Initial task description.", null, timeProvider);
        Guid originalVersion = task.Version;
        timeProvider.Advance(TimeSpan.FromMinutes(5));

        string? error = task.Update(
            "Updated title",
            "Updated task description.",
            TaskItemStatus.InProgress,
            Start.AddDays(2),
            timeProvider);

        error.ShouldBeNull();
        task.Status.ShouldBe(TaskItemStatus.InProgress);
        task.UpdatedAt.ShouldBe(Start.AddMinutes(5));
        task.Version.ShouldNotBe(originalVersion);
    }

    [Test]
    public void Complete_Rejects_A_Cancelled_Task()
    {
        FakeTimeProvider timeProvider = new(Start);
        TaskItem task = TaskItem.Create("Cancelled task", "A task that cannot be completed.", null, timeProvider);
        task.Update(
            task.Title,
            task.Description,
            TaskItemStatus.Cancelled,
            task.DueDate,
            timeProvider).ShouldBeNull();

        string? error = task.Complete(timeProvider);

        error.ShouldBe("A cancelled task cannot be completed.");
        task.Status.ShouldBe(TaskItemStatus.Cancelled);
    }

    [Test]
    public void Update_Rejects_Changing_A_Terminal_Task()
    {
        FakeTimeProvider timeProvider = new(Start);
        TaskItem task = TaskItem.Create("Terminal task", "A completed task stays completed.", null, timeProvider);
        task.Complete(timeProvider).ShouldBeNull();
        Guid completedVersion = task.Version;

        string? error = task.Update(
            task.Title,
            task.Description,
            TaskItemStatus.InProgress,
            task.DueDate,
            timeProvider);

        error.ShouldBe("A task cannot transition from Done to InProgress.");
        task.Status.ShouldBe(TaskItemStatus.Done);
        task.Version.ShouldBe(completedVersion);
    }

    [Test]
    public void Update_Normalizes_Text_And_Due_Date()
    {
        FakeTimeProvider timeProvider = new(Start);
        TaskItem task = TaskItem.Create("Initial title", "Initial task description.", null, timeProvider);
        DateTimeOffset dueDate = Start.ToOffset(TimeSpan.FromHours(-4)).AddDays(1);

        task.Update(
            "  Normalized title  ",
            "  Normalized task description.  ",
            TaskItemStatus.InProgress,
            dueDate,
            timeProvider).ShouldBeNull();

        task.Title.ShouldBe("Normalized title");
        task.Description.ShouldBe("Normalized task description.");
        task.DueDate.ShouldBe(dueDate.ToUniversalTime());
    }

    [Test]
    public void Complete_Is_Idempotent()
    {
        FakeTimeProvider timeProvider = new(Start);
        TaskItem task = TaskItem.Create("Complete task", "Complete this task only once.", null, timeProvider);
        task.Complete(timeProvider).ShouldBeNull();
        Guid completedVersion = task.Version;
        DateTimeOffset completedAt = task.UpdatedAt;

        timeProvider.Advance(TimeSpan.FromMinutes(5));
        task.Complete(timeProvider).ShouldBeNull();

        task.Version.ShouldBe(completedVersion);
        task.UpdatedAt.ShouldBe(completedAt);
    }

    [Test]
    public void Create_Rejects_Content_That_Is_Too_Short_After_Trimming()
    {
        FakeTimeProvider timeProvider = new(Start);

        Should.Throw<ArgumentOutOfRangeException>(() =>
            TaskItem.Create("  a  ", "A valid task description.", null, timeProvider));
        Should.Throw<ArgumentOutOfRangeException>(() =>
            TaskItem.Create("Valid title", "  short  ", null, timeProvider));
    }
}
