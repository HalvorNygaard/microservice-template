using System.Text.Json.Serialization;

namespace ModernMicroservice.Features.Tasks;

internal sealed class TaskItem
{
    private TaskItem()
    {
    }

    private TaskItem(Guid id, string title, string description, DateTimeOffset now)
    {
        Id = id;
        Title = title;
        Description = description;
        Status = TaskItemStatus.Todo;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public TaskItemStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static TaskItem Create(string title, string description, TimeProvider timeProvider)
    {
        (title, description) = NormalizeAndValidate(title, description);
        DateTimeOffset now = timeProvider.GetUtcNow();
        return new TaskItem(Guid.CreateVersion7(now), title, description, now);
    }

    public bool Complete(TimeProvider timeProvider)
    {
        if (Status == TaskItemStatus.Done)
        {
            return false;
        }

        Status = TaskItemStatus.Done;
        UpdatedAt = timeProvider.GetUtcNow();
        return true;
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

[JsonConverter(typeof(JsonStringEnumConverter<TaskItemStatus>))]
public enum TaskItemStatus
{
    Todo = 0,
    Done = 1
}
