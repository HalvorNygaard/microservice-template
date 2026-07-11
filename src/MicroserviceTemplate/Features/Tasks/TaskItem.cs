namespace MicroserviceTemplate.Features.Tasks;

internal sealed class TaskItem
{
    private TaskItem()
    {
    }

    private TaskItem(Guid id, string title, string description, DateTimeOffset? dueDate, DateTimeOffset now)
    {
        Id = id;
        Title = title;
        Description = description;
        DueDate = dueDate;
        Status = TaskItemStatus.Todo;
        CreatedAt = now;
        UpdatedAt = now;
        Version = Guid.CreateVersion7(now);
    }

    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public TaskItemStatus Status { get; private set; }
    public DateTimeOffset? DueDate { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid Version { get; private set; }

    public static TaskItem Create(string title, string description, DateTimeOffset? dueDate, TimeProvider timeProvider)
    {
        (title, description) = NormalizeAndValidate(title, description);
        DateTimeOffset now = timeProvider.GetUtcNow();
        return new TaskItem(
            Guid.CreateVersion7(now),
            title,
            description,
            dueDate?.ToUniversalTime(),
            now);
    }

    public string? Update(
        string title,
        string description,
        TaskItemStatus status,
        DateTimeOffset? dueDate,
        TimeProvider timeProvider)
    {
        (title, description) = NormalizeAndValidate(title, description);
        if (!CanTransition(Status, status))
        {
            return $"A task cannot transition from {Status} to {status}.";
        }

        Title = title;
        Description = description;
        Status = status;
        DueDate = dueDate?.ToUniversalTime();
        Touch(timeProvider.GetUtcNow());
        return null;
    }

    public string? Complete(TimeProvider timeProvider)
    {
        if (Status == TaskItemStatus.Cancelled)
        {
            return "A cancelled task cannot be completed.";
        }

        if (Status != TaskItemStatus.Done)
        {
            Status = TaskItemStatus.Done;
            Touch(timeProvider.GetUtcNow());
        }

        return null;
    }

    private static bool CanTransition(TaskItemStatus current, TaskItemStatus next) =>
        current == next || current switch
        {
            TaskItemStatus.Todo => next is TaskItemStatus.InProgress or TaskItemStatus.Done or TaskItemStatus.Cancelled,
            TaskItemStatus.InProgress => next is TaskItemStatus.Done or TaskItemStatus.Cancelled,
            _ => false
        };

    private void Touch(DateTimeOffset now)
    {
        UpdatedAt = now;
        Version = Guid.CreateVersion7(now);
    }

    private static (string Title, string Description) NormalizeAndValidate(string title, string description)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(description);

        string normalizedTitle = title.Trim();
        string normalizedDescription = description.Trim();
        if (normalizedTitle.Length is < 3 or > 200)
        {
            throw new ArgumentOutOfRangeException(nameof(title), "A task title must contain 3 to 200 characters after trimming.");
        }

        if (normalizedDescription.Length is < 10 or > 2000)
        {
            throw new ArgumentOutOfRangeException(nameof(description), "A task description must contain 10 to 2000 characters after trimming.");
        }

        return (normalizedTitle, normalizedDescription);
    }
}

public enum TaskItemStatus
{
    Todo = 0,
    InProgress = 1,
    Done = 2,
    Cancelled = 3
}
