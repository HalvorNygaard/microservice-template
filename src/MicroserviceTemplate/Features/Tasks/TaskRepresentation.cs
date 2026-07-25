using System.Linq.Expressions;

namespace ModernMicroservice.Features.Tasks;

public sealed record TaskRepresentation(
    Guid Id,
    string Title,
    string Description,
    TaskItemStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

internal static class TaskMappings
{
    internal static readonly Expression<Func<TaskItem, TaskRepresentation>> Projection = task => new(
        task.Id,
        task.Title,
        task.Description,
        task.Status,
        task.CreatedAt,
        task.UpdatedAt);

    internal static TaskRepresentation ToRepresentation(this TaskItem task) => new(
        task.Id,
        task.Title,
        task.Description,
        task.Status,
        task.CreatedAt,
        task.UpdatedAt);
}
