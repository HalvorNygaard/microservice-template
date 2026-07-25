using ModernMicroservice.Features.Tasks;
using Microsoft.Extensions.Time.Testing;

namespace ModernMicroservice.UnitTests.Tasks;

public sealed class TaskItemTests
{
    private static readonly DateTimeOffset Start = new(2026, 7, 11, 8, 0, 0, TimeSpan.Zero);

    [Test]
    public void CreateNormalizesContentAndUsesInjectedTime()
    {
        FakeTimeProvider timeProvider = new(Start);

        TaskItem task = TaskItem.Create(
            "  Write tests  ",
            "  Verify domain behavior clearly.  ",
            timeProvider);

        task.Id.ToString("N")[12].ShouldBe('7');
        task.Title.ShouldBe("Write tests");
        task.Description.ShouldBe("Verify domain behavior clearly.");
        task.CreatedAt.ShouldBe(Start);
        task.UpdatedAt.ShouldBe(Start);
        task.Status.ShouldBe(TaskItemStatus.Todo);
    }

    [Test]
    public void CompleteAdvancesTheStateAndTimestamp()
    {
        FakeTimeProvider timeProvider = new(Start);
        TaskItem task = TaskItem.Create("Complete task", "Complete this task once.", timeProvider);
        timeProvider.Advance(TimeSpan.FromMinutes(5));

        bool changed = task.Complete(timeProvider);

        changed.ShouldBeTrue();
        task.Status.ShouldBe(TaskItemStatus.Done);
        task.UpdatedAt.ShouldBe(Start.AddMinutes(5));
    }

    [Test]
    public void CompleteIsIdempotent()
    {
        FakeTimeProvider timeProvider = new(Start);
        TaskItem task = TaskItem.Create("Complete task", "Complete this task only once.", timeProvider);
        task.Complete(timeProvider).ShouldBeTrue();
        DateTimeOffset completedAt = task.UpdatedAt;

        timeProvider.Advance(TimeSpan.FromMinutes(5));

        task.Complete(timeProvider).ShouldBeFalse();
        task.UpdatedAt.ShouldBe(completedAt);
    }

    [Test]
    public void CreateRejectsContentThatIsTooShortAfterTrimming()
    {
        FakeTimeProvider timeProvider = new(Start);

        Should.Throw<ArgumentOutOfRangeException>(() =>
            TaskItem.Create("  a  ", "A valid task description.", timeProvider));
        Should.Throw<ArgumentOutOfRangeException>(() =>
            TaskItem.Create("Valid title", "  short  ", timeProvider));
    }
}
