namespace MicroserviceTemplate.Features.Tasks.Models;

public class TaskItem
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public required string Title { get; set; }
    public required string Description { get; set; }
    public TaskStatus Status { get; set; } = TaskStatus.Todo;
    public DateTimeOffset? DueDate { get; set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public void UpdateTimestamp()
    {
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

public enum TaskStatus
{
    Todo = 0,
    InProgress = 1,
    Done = 2,
    Cancelled = 3
}
